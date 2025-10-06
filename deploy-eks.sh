#!/bin/bash

# FCG Identity System - Deploy Script
# Substituto do access-services.ps1 para AWS EKS

set -e

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

NAMESPACE="identity-system"

show_usage() {
    echo -e "${BLUE}FCG Identity System - AWS EKS Deploy${NC}"
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  deploy    - Deploy all services to EKS"
    echo "  status    - Check status of all services"
    echo "  logs      - Show logs from all services" 
    echo "  test      - Test connectivity to all endpoints"
    echo "  cleanup   - Remove all services and namespace"
    echo "  restart   - Restart all deployments"
    echo ""
}

deploy_services() {
    echo -e "${BLUE}🚀 Deploying FCG Identity System to EKS...${NC}"
    
    # Criar namespace se não existir
    kubectl create namespace $NAMESPACE --dry-run=client -o yaml | kubectl apply -f -
    
    # Aplicar todos os manifestos
    echo -e "${YELLOW}📦 Applying Kubernetes manifests...${NC}"
    kubectl apply -f k8s/production/ -n $NAMESPACE
    
    echo -e "${GREEN}✅ Deploy completed!${NC}"
    echo -e "${YELLOW}⏳ Waiting for pods to be ready...${NC}"
    
    # Aguardar pods ficarem prontos
    kubectl wait --for=condition=ready pods --all -n $NAMESPACE --timeout=300s
    
    show_status
}

show_status() {
    echo -e "${BLUE}📊 Current Status:${NC}"
    echo ""
    
    echo -e "${YELLOW}Pods:${NC}"
    kubectl get pods -n $NAMESPACE -o wide
    echo ""
    
    echo -e "${YELLOW}Services:${NC}"
    kubectl get svc -n $NAMESPACE
    echo ""
    
    echo -e "${YELLOW}Ingress:${NC}"
    kubectl get ingress -n $NAMESPACE
    echo ""
    
    echo -e "${YELLOW}LoadBalancer IP:${NC}"
    INGRESS_IP=$(kubectl get ingress -n $NAMESPACE -o jsonpath='{.items[0].status.loadBalancer.ingress[0].hostname}' 2>/dev/null || echo "Pending...")
    echo "External IP: $INGRESS_IP"
}

show_logs() {
    echo -e "${BLUE}📋 Service Logs:${NC}"
    
    echo -e "${YELLOW}Keycloak logs:${NC}"
    kubectl logs -n $NAMESPACE -l app=keycloak --tail=20
    
    echo -e "${YELLOW}Kong logs:${NC}"
    kubectl logs -n $NAMESPACE -l app=kong --tail=20
    
    echo -e "${YELLOW}Identity API logs:${NC}"
    kubectl logs -n $NAMESPACE -l app=identity-api --tail=20
}

test_connectivity() {
    echo -e "${BLUE}🔍 Testing Connectivity:${NC}"
    
    INGRESS_IP=$(kubectl get ingress -n $NAMESPACE -o jsonpath='{.items[0].status.loadBalancer.ingress[0].hostname}' 2>/dev/null)
    
    if [ -z "$INGRESS_IP" ]; then
        echo -e "${RED}❌ No LoadBalancer IP found${NC}"
        return 1
    fi
    
    echo "Testing endpoints with IP: $INGRESS_IP"
    
    # Testar endpoints
    echo -e "${YELLOW}Testing Keycloak...${NC}"
    if curl -k -s -o /dev/null -w "%{http_code}" https://keycloak.fcg-identity.com/realms/master | grep -q "200\|404"; then
        echo -e "${GREEN}✅ Keycloak: OK${NC}"
    else
        echo -e "${RED}❌ Keycloak: Failed${NC}"
    fi
    
    echo -e "${YELLOW}Testing Kong...${NC}"
    if curl -k -s -o /dev/null -w "%{http_code}" https://kong.fcg-identity.com/ | grep -q "200\|404"; then
        echo -e "${GREEN}✅ Kong: OK${NC}"
    else
        echo -e "${RED}❌ Kong: Failed${NC}"
    fi
    
    echo -e "${YELLOW}Testing Identity API...${NC}"
    if curl -k -s -o /dev/null -w "%{http_code}" https://api.fcg-identity.com/health | grep -q "200"; then
        echo -e "${GREEN}✅ Identity API: OK${NC}"
    else
        echo -e "${RED}❌ Identity API: Failed${NC}"
    fi
    
    echo -e "${YELLOW}Testing Kong Admin...${NC}"
    if curl -k -s -o /dev/null -w "%{http_code}" https://kong-admin.fcg-identity.com/ | grep -q "200\|404"; then
        echo -e "${GREEN}✅ Kong Admin: OK${NC}"
    else
        echo -e "${RED}❌ Kong Admin: Failed${NC}"
    fi
}

cleanup_services() {
    echo -e "${BLUE}🗑️  Cleaning up FCG Identity System...${NC}"
    
    # Remover todos os recursos
    kubectl delete -f k8s/production/ -n $NAMESPACE --ignore-not-found=true
    
    # Remover namespace
    kubectl delete namespace $NAMESPACE --ignore-not-found=true
    
    echo -e "${GREEN}✅ Cleanup completed!${NC}"
}

restart_services() {
    echo -e "${BLUE}🔄 Restarting all deployments...${NC}"
    
    kubectl rollout restart deployment -n $NAMESPACE
    
    echo -e "${YELLOW}⏳ Waiting for rollout to complete...${NC}"
    kubectl rollout status deployment --all -n $NAMESPACE
    
    echo -e "${GREEN}✅ Restart completed!${NC}"
}

# Main execution
case "$1" in
    deploy)
        deploy_services
        ;;
    status)
        show_status
        ;;
    logs)
        show_logs
        ;;
    test)
        test_connectivity
        ;;
    cleanup)
        cleanup_services
        ;;
    restart)
        restart_services
        ;;
    *)
        show_usage
        exit 1
        ;;
esac