#!/bin/bash

set -e

# ============================================
# Install Kong Ingress Controller on EKS
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

CLUSTER_NAME="fcg-identity-cluster"
NAMESPACE="kong"

echo "=========================================="
echo "  Installing Kong Ingress Controller"
echo "=========================================="
echo ""

# Add Kong Helm repository
print_status "Adding Kong Helm repository..."
helm repo add kong https://charts.konghq.com
helm repo update

# Create namespace
kubectl create namespace $NAMESPACE --dry-run=client -o yaml | kubectl apply -f -

# Install Kong using Helm
print_status "Installing Kong Ingress Controller..."

helm install kong kong/ingress \
  --namespace $NAMESPACE \
  --set ingressController.installCRDs=false \
  --set gateway.env.database=off \
  --set gateway.env.router_flavor=traditional \
  --set gateway.proxy.type=LoadBalancer \
  --set gateway.proxy.annotations."service\.beta\.kubernetes\.io/aws-load-balancer-type"="nlb" \
  --set gateway.proxy.annotations."service\.beta\.kubernetes\.io/aws-load-balancer-scheme"="internet-facing"

# Wait for Kong to be ready
print_status "Waiting for Kong to be ready..."
kubectl wait --namespace $NAMESPACE \
  --for=condition=ready pod \
  --selector=app.kubernetes.io/name=ingress-kong \
  --timeout=300s

# Get LoadBalancer URL
echo ""
print_status "Kong Ingress Controller installed successfully!"
echo ""
echo "🌐 Kong Proxy LoadBalancer:"
kubectl get svc -n $NAMESPACE kong-gateway-proxy -o jsonpath='{.status.loadBalancer.ingress[0].hostname}'
echo ""
echo ""
echo "📍 Next Steps:"
echo "  1. Note the LoadBalancer hostname above"
echo "  2. Create DNS records pointing to this LoadBalancer"
echo "  3. Deploy Identity Service: ./03-deploy-identity-service.sh"
echo ""
