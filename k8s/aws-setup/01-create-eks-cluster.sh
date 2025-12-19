#!/bin/bash

set -e

# ============================================
# FCG Identity Service - EKS Cluster Setup
# ============================================

# Colors
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

# ============================================
# Configurações do Cluster
# ============================================

CLUSTER_NAME="fcg-identity-cluster"
REGION="sa-east-1"  # São Paulo
K8S_VERSION="1.34"
NODE_TYPE="m7i-flex.large"
NODE_MIN=2
NODE_MAX=4
NODE_DESIRED=2

echo "=========================================="
echo "  Creating EKS Cluster"
echo "=========================================="
echo ""
echo "Cluster Name: $CLUSTER_NAME"
echo "Region: $REGION"
echo "Kubernetes Version: $K8S_VERSION"
echo "Node Type: $NODE_TYPE"
echo "Nodes: $NODE_DESIRED (min: $NODE_MIN, max: $NODE_MAX)"
echo ""

read -p "Proceed with cluster creation? (y/n) " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    print_warning "Aborted by user"
    exit 0
fi

# ============================================
# Verificar pré-requisitos
# ============================================

echo ""
print_status "Checking prerequisites..."

# Verificar AWS CLI
if ! command -v aws &> /dev/null; then
    print_error "AWS CLI not found! Install: https://aws.amazon.com/cli/"
    exit 1
fi
print_status "AWS CLI: $(aws --version)"

# Verificar eksctl
if ! command -v eksctl &> /dev/null; then
    print_error "eksctl not found! Install: https://eksctl.io/installation/"
    exit 1
fi
print_status "eksctl: $(eksctl version)"

# Verificar kubectl
if ! command -v kubectl &> /dev/null; then
    print_warning "kubectl not found. Will be installed automatically by eksctl."
else
    print_status "kubectl: $(kubectl version --client --short 2>/dev/null || kubectl version --client)"
fi

# Verificar credenciais AWS
if ! aws sts get-caller-identity &> /dev/null; then
    print_error "AWS credentials not configured! Run: aws configure"
    exit 1
fi

AWS_ACCOUNT=$(aws sts get-caller-identity --query Account --output text)
AWS_USER=$(aws sts get-caller-identity --query Arn --output text)
print_status "AWS Account: $AWS_ACCOUNT"
print_status "AWS User: $AWS_USER"

# ============================================
# Criar cluster EKS
# ============================================

echo ""
echo "=========================================="
echo "  Creating EKS Cluster (15-20 minutes)"
echo "=========================================="
echo ""

eksctl create cluster \
  --name $CLUSTER_NAME \
  --region $REGION \
  --version $K8S_VERSION \
  --nodegroup-name standard-workers \
  --node-type $NODE_TYPE \
  --nodes $NODE_DESIRED \
  --nodes-min $NODE_MIN \
  --nodes-max $NODE_MAX \
  --managed

if [ $? -eq 0 ]; then
    print_status "EKS Cluster created successfully!"
else
    print_error "Failed to create EKS cluster"
    exit 1
fi

# ============================================
# Configurar kubectl
# ============================================

echo ""
print_status "Configuring kubectl..."

aws eks update-kubeconfig \
  --region $REGION \
  --name $CLUSTER_NAME

kubectl cluster-info
kubectl get nodes

# ============================================
# Summary
# ============================================

echo ""
echo "=========================================="
echo "  ✅ EKS Cluster Ready!"
echo "=========================================="
echo ""
echo "📊 Cluster Information:"
echo "  Name: $CLUSTER_NAME"
echo "  Region: $REGION"
echo "  Kubernetes: $K8S_VERSION"
echo "  Nodes: $(kubectl get nodes --no-headers | wc -l)"
echo ""
echo " Next Steps:"
echo "  1. Install Kong Ingress Controller:"
echo "     ./02-install-kong.sh"
echo ""
echo "  2. Deploy Identity Service:"
echo "     ./03-deploy-identity-service.sh"
echo ""
echo "🔍 Useful Commands:"
echo "  kubectl get nodes"
echo "  kubectl get pods -A"
echo "  kubectl get svc -A"
echo ""
