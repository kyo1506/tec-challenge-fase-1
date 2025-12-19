#!/bin/bash

set -e

# ============================================
# Deploy FCG Identity Service to EKS
# ============================================

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

print_status() {
    echo -e "${GREEN}✓${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

print_error() {
    echo -e "${RED}✗${NC} $1"
}

CLUSTER_NAME="fcg-identity-cluster"
REGION="sa-east-1"
AWS_ACCOUNT=$(aws sts get-caller-identity --query Account --output text)
ECR_REPO="${AWS_ACCOUNT}.dkr.ecr.${REGION}.amazonaws.com/fcg-identity-api"

echo "=========================================="
echo "  Deploy Identity Service to EKS"
echo "=========================================="
echo ""

# ============================================
# 1. Create ECR Repository
# ============================================

echo "1. Creating ECR Repository..."

aws ecr create-repository \
    --repository-name fcg-identity-api \
    --region $REGION \
    --image-scanning-configuration scanOnPush=true \
    2>/dev/null || print_warning "ECR repository already exists"

print_status "ECR Repository: $ECR_REPO"

# ============================================
# 2. Build and Push Docker Image
# ============================================

echo ""
echo "2. Building and pushing Docker image..."
echo ""

read -p "Build and push new image? (y/n) " -n 1 -r
echo ""
if [[ $REPLY =~ ^[Yy]$ ]]; then
    # Navigate to project root
    cd ../..
    
    # Login to ECR
    print_status "Logging in to ECR..."
    aws ecr get-login-password --region $REGION | docker login --username AWS --password-stdin $ECR_REPO
    
    # Build image
    print_status "Building Docker image..."
    docker build -t fcg-identity-api:latest .
    
    # Tag image
    print_status "Tagging image..."
    docker tag fcg-identity-api:latest $ECR_REPO:latest
    docker tag fcg-identity-api:latest $ECR_REPO:$(git rev-parse --short HEAD 2>/dev/null || echo "manual")
    
    # Push image
    print_status "Pushing to ECR..."
    docker push $ECR_REPO:latest
    docker push $ECR_REPO:$(git rev-parse --short HEAD 2>/dev/null || echo "manual")
    
    print_status "Image pushed successfully!"
    
    cd k8s/aws-setup
else
    print_warning "Skipping image build. Make sure image exists in ECR!"
fi

# ============================================
# 3. Update deployment with ECR image
# ============================================

echo ""
echo "3. Updating deployment configuration..."

# Update image in deployment.yaml
sed -i.bak "s|image:.*fcg-identity-api.*|image: ${ECR_REPO}:latest|g" ../production/deployment.yaml

print_status "Deployment updated with ECR image"

# ============================================
# 4. Deploy to Kubernetes
# ============================================

echo ""
echo "4. Deploying to Kubernetes..."
echo ""

cd ../production

# Apply in order
print_status "Creating namespace..."
kubectl apply -f namespace.yaml

print_status "Creating secrets..."
kubectl apply -f secrets.yaml

print_status "Creating configmaps..."
kubectl apply -f configmaps.yaml

print_status "Creating storage class..."
kubectl apply -f storage-class.yaml

print_status "Creating volumes..."
kubectl apply -f volumes.yaml

print_status "Deploying database..."
kubectl apply -f databases.yaml

echo ""
print_warning "Waiting for database to be ready (2 minutes)..."
kubectl wait --for=condition=ready pod -l app=keycloak-db -n identity-system --timeout=300s

print_status "Deploying Keycloak..."
kubectl apply -f infrastructure.yaml

echo ""
print_warning "Waiting for Keycloak to be ready (5 minutes)..."
kubectl wait --for=condition=ready pod -l app=keycloak -n identity-system --timeout=600s

print_status "Deploying Identity API..."
kubectl apply -f deployment.yaml

print_status "Creating services..."
kubectl apply -f services.yaml

print_status "Deploying Ingress (Kong)..."
kubectl apply -f ingress-aws.yaml

print_status "Deploying HPA..."
kubectl apply -f hpa.yaml

print_status "Deploying observability stack (optional)..."
kubectl apply -f observability.yaml 2>/dev/null || print_warning "Observability deployment failed (optional)"

# ============================================
# 5. Verify deployment
# ============================================

echo ""
echo "=========================================="
echo "  Verifying Deployment"
echo "=========================================="
echo ""

kubectl get pods -n identity-system
echo ""
kubectl get svc -n identity-system
echo ""
kubectl get ingress -n identity-system
echo ""

# ============================================
# 6. Get access URLs
# ============================================

echo ""
echo "=========================================="
echo "  ✅ Deployment Complete!"
echo "=========================================="
echo ""

KONG_LB=$(kubectl get svc -n kong kong-gateway-proxy -o jsonpath='{.status.loadBalancer.ingress[0].hostname}')

echo "🌐 Access URLs:"
echo ""
echo "Kong LoadBalancer: $KONG_LB"
echo ""
echo "To access services, configure DNS:"
echo "  keycloak.fcg-identity.com  →  $KONG_LB"
echo "  api.fcg-identity.com       →  $KONG_LB"
echo ""
echo "Or use port-forward for testing:"
echo "  kubectl port-forward -n identity-system svc/keycloak-service 8080:8080"
echo "  kubectl port-forward -n identity-system svc/identity-api-service 5000:80"
echo ""
echo "📚 Next Steps:"
echo "  1. Configure DNS records"
echo "  2. Setup Keycloak realm: see KEYCLOAK_MANUAL_SETUP.md"
echo "  3. Test the API endpoints"
echo ""
