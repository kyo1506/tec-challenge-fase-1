# FCG Identity System - AWS EKS Setup Guide

## 🚀 **Pré-requisitos**

### 1. Instalar Ferramentas
```bash
# AWS CLI
curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
unzip awscliv2.zip
sudo ./aws/install

# eksctl
curl --silent --location "https://github.com/weaveworks/eksctl/releases/latest/download/eksctl_$(uname -s)_amd64.tar.gz" | tar xz -C /tmp
sudo mv /tmp/eksctl /usr/local/bin

# kubectl (caso não tenha)
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
chmod +x kubectl
sudo mv kubectl /usr/local/bin/

# Helm
curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash
```

## 🏗️ **Setup do Cluster EKS**

### 1. Pré-verificações (Essencial para evitar timeouts)
```bash
# Verificar quotas da conta AWS
aws service-quotas get-service-quota --service-code eks --quota-code L-1194D53C --region sa-east-1
aws service-quotas get-service-quota --service-code ec2 --quota-code L-1216C47A --region sa-east-1

# Verificar se as zonas estão disponíveis
aws ec2 describe-availability-zones --region sa-east-1 --zone-names sa-east-1a sa-east-1b

# Verificar permissões IAM necessárias
aws sts get-caller-identity
```

### 2. Criar Cluster EKS com Kubernetes 1.34 (Amazon Linux + m7i-flex.large)
```bash
# 🚀 CONFIGURAÇÃO OTIMIZADA: Amazon Linux + m7i-flex.large
# m7i-flex.large (2 vCPUs, 8 GB RAM) - Instâncias flexíveis Intel
eksctl create cluster \
  --name fcg-identity \
  --region sa-east-1 \
  --version 1.34 \
  --nodegroup-name standard-workers \
  --node-type m7i-flex.large \
  --nodes 3 \
  --nodes-min 2 \
  --nodes-max 5 \
  --managed \
  --full-ecr-access \
  --with-oidc \
  --zones sa-east-1a,sa-east-1b \
  --timeout=45m \
  --verbose 2
```

### Alternativa: Configuração Separada (Amazon Linux + m7i-flex.large)
```bash
# 1. Criar cluster sem nodes primeiro
eksctl create cluster \
  --name fcg-identity \
  --region sa-east-1 \
  --version 1.34 \
  --without-nodegroup \
  --with-oidc \
  --zones sa-east-1a,sa-east-1b \
  --timeout=30m

# 2. Adicionar nodegroup com Amazon Linux após cluster estar pronto
eksctl create nodegroup \
  --cluster fcg-identity \
  --region sa-east-1 \
  --name standard-workers \
  --node-type m7i-flex.large \
  --nodes 3 \
  --nodes-min 2 \
  --nodes-max 5 \
  --managed \
  --full-ecr-access
```

### 3. Verificar Criação do Cluster
```bash
# Aguardar cluster ficar ativo (pode demorar até 15 minutos)
eksctl get cluster fcg-identity --region sa-east-1

# Configurar kubeconfig
aws eks update-kubeconfig --region sa-east-1 --name fcg-identity

# Verificar nodes
kubectl get nodes -o wide

# Verificar addons padrão instalados
kubectl get pods -n kube-system
eksctl get addons --cluster fcg-identity --region sa-east-1
```

### 4. Troubleshooting Comum
```bash
# Se o cluster não criar, verificar logs do CloudFormation
aws cloudformation describe-stack-events --stack-name eksctl-fcg-identity-cluster --region sa-east-1

# Verificar se não há recursos conflitantes
aws ec2 describe-vpcs --region sa-east-1 --filters "Name=tag:Name,Values=eksctl-fcg-identity-cluster/*"

# Limpar recursos órfãos se necessário
eksctl delete cluster --name fcg-identity --region sa-east-1 --wait
```

## 🔧 **Versões dos Componentes - Kubernetes 1.34**

### Addons Compatíveis Testados:
- **EBS CSI Driver**: v1.49.0-eksbuild.1 (Mais recente para K8s 1.34)
- **VPC CNI**: v1.20.3-eksbuild.1 (Instalado automaticamente)
- **CoreDNS**: v1.11.3-eksbuild.2 (Atualização automática)
- **Kube-proxy**: v1.34.1-eksbuild.2 (Compatível com cluster)

### Helm Charts Otimizados:
- **Nginx Ingress**: v4.11+ (Compatível com K8s 1.34)
- **Cert-manager**: v1.15.3 (Suporte completo para K8s 1.34)
- **Prometheus Stack**: v61.0+ (Otimizado para recursos)
- **AWS Load Balancer Controller**: v1.8+ (Suporte nativo para K8s 1.34)

```bash
# Configuração equilibrada (custo x performance)
eksctl create cluster \
  --name fcg-identity \
  --region sa-east-1 \
  --nodes 3 \
  --nodes-min 2 \
  --nodes-max 5 \
  --node-type t3.large \
  --managed \
  --zones sa-east-1a,sa-east-1b \
  --node-volume-size 30

# Alternativa para alta carga (se necessário)
eksctl create cluster \
  --name fcg-identity-prod \
  --region sa-east-1 \
  --nodes 3 \
  --nodes-min 2 \
  --nodes-max 6 \
  --node-type c5.xlarge \
  --managed \
  --zones sa-east-1a,sa-east-1b \
  --node-volume-size 50
```

### 2. Configurar kubeconfig
```bash
aws eks update-kubeconfig --region sa-east-1 --name fcg-identity
```

### 3. Verificar Cluster
```bash
# Verificar nodes e recursos
kubectl get nodes -o wide
kubectl top nodes

# Verificar namespaces e addons
kubectl get namespaces
kubectl get pods -n kube-system

# Verificar capacidade total do cluster
kubectl describe nodes | grep -E "Name:|cpu:|memory:"

# Instalar metrics-server se não estiver funcionando
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml
```

### 4. Testar Recursos do Cluster
```bash
# Verificar se o cluster suporta nossa carga
kubectl run test-pod --image=nginx --requests='cpu=100m,memory=128Mi' --limits='cpu=200m,memory=256Mi' --rm -it --restart=Never

# Verificar scheduling de pods
kubectl get events --sort-by='.metadata.creationTimestamp'
```

## 🔧 **Instalar Addons via Helm**

### 1. Adicionar Repositórios Helm
```bash
# Nginx Ingress Controller
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx

# Cert Manager (para SSL)
helm repo add jetstack https://charts.jetstack.io

# Atualizar repositórios
helm repo update
```

### 2. Instalar Nginx Ingress Controller (Kubernetes 1.34 Otimizado)
```bash
# Nginx Ingress Controller v4.11+ compatível com K8s 1.34
helm install nginx-ingress ingress-nginx/ingress-nginx \
  --namespace ingress-nginx \
  --create-namespace \
  --set controller.service.type=LoadBalancer \
  --set controller.replicaCount=1 \
  --set controller.resources.requests.cpu=100m \
  --set controller.resources.requests.memory=128Mi \
  --set controller.resources.limits.cpu=200m \
  --set controller.resources.limits.memory=256Mi \
  --set controller.service.annotations."service\.beta\.kubernetes\.io/aws-load-balancer-type"="nlb" \
  --set controller.service.annotations."service\.beta\.kubernetes\.io/aws-load-balancer-cross-zone-load-balancing-enabled"="true"
```

### 3. Aguardar LoadBalancer
```bash
kubectl get svc -n ingress-nginx -w
# Aguarde até aparecer EXTERNAL-IP (LoadBalancer da AWS)
```

### 4. Verificar Status
```bash
kubectl get pods -n ingress-nginx
kubectl get svc -n ingress-nginx
helm list -n ingress-nginx
```

## 📦 **Setup do ECR (Container Registry)**

### 1. Criar Repository
```bash
aws ecr create-repository --repository-name fcg-identity-api --region sa-east-1
```

### 2. Obter Login Token
```bash
aws ecr get-login-password --region sa-east-1 | docker login --username AWS --password-stdin 208452488125.dkr.ecr.sa-east-1.amazonaws.com
```

### 3. Build e Push da Imagem
```bash
# Build
docker build -t fcg-identity-api .

# Tag
docker tag fcg-identity-api:latest 208452488125.dkr.ecr.sa-east-1.amazonaws.com/fcg-identity-api:latest

# Push
docker push 208452488125.dkr.ecr.sa-east-1.amazonaws.com/fcg-identity-api:latest
```

### 4. Atualizar Deployment
Editar `k8s/production/deployment.yaml` e substituir:
```yaml
image: identity-api:latest
```
Por:
```yaml
image: 208452488125.dkr.ecr.sa-east-1.amazonaws.com/fcg-identity-api:latest
```

## � **Scripts de Deploy**

Foram criados dois scripts para facilitar o deploy e gerenciamento:

### **deploy-eks.sh** (Linux/Mac)
```bash
chmod +x deploy-eks.sh
./deploy-eks.sh <command>
```

### **deploy-eks.ps1** (Windows PowerShell)
```powershell
.\deploy-eks.ps1 -Command <command>
```

### Comandos Disponíveis:
- **deploy**: Faz deploy completo de todos os serviços
- **status**: Mostra status de pods, serviços e ingress
- **logs**: Exibe logs dos últimos 20 eventos de cada serviço
- **test**: Testa conectividade com todos os endpoints
- **cleanup**: Remove todos os recursos e namespace
- **restart**: Reinicia todos os deployments

---

## �🚀 **Deploy da Aplicação**

### Opção 1: Usar Script de Deploy (Recomendado)
```bash
# Linux/Mac - Dar permissão e executar
chmod +x deploy-eks.sh
./deploy-eks.sh deploy

# Windows PowerShell
.\deploy-eks.ps1 -Command deploy
```

### Opção 2: Deploy Manual dos Manifestos
```bash
# Criar namespace
kubectl create namespace identity-system

# Aplicar todos os manifestos
kubectl apply -f k8s/production/ -n identity-system
```

### 2. Verificar Status
```bash
# Usando script (recomendado)
./deploy-eks.sh status
# ou
.\deploy-eks.ps1 -Command status

# Manualmente
kubectl get pods -n identity-system -o wide
kubectl get svc -n identity-system
kubectl get ingress -n identity-system
```

### 3. Ver Logs (se necessário)
```bash
# Usando script
./deploy-eks.sh logs
# ou  
.\deploy-eks.ps1 -Command logs

# Manualmente
kubectl logs -f deployment/identity-api -n identity-system
kubectl logs -f deployment/keycloak -n identity-system
kubectl logs -f deployment/kong -n identity-system
```

## 🌐 **Configurar DNS**

### 1. No Route 53 (ou seu DNS provider)
Criar registros CNAME apontando para o LoadBalancer:

```
kong.fcg-identity.com -> [EXTERNAL-IP-DO-LOADBALANCER]
keycloak.fcg-identity.com -> [EXTERNAL-IP-DO-LOADBALANCER]
konga.fcg-identity.com -> [EXTERNAL-IP-DO-LOADBALANCER]
api.fcg-identity.com -> [EXTERNAL-IP-DO-LOADBALANCER]
kong-admin.fcg-identity.com -> [EXTERNAL-IP-DO-LOADBALANCER]
```

### 2. Testar Conectividade
```bash
# Usando script (recomendado - testa todos os endpoints)
./deploy-eks.sh test
# ou
.\deploy-eks.ps1 -Command test

# Manualmente
LOAD_BALANCER_IP=$(kubectl get ingress -n identity-system -o jsonpath='{.items[0].status.loadBalancer.ingress[0].hostname}')
echo "LoadBalancer IP: $LOAD_BALANCER_IP"

curl -k https://keycloak.fcg-identity.com/realms/master
curl -k https://kong.fcg-identity.com/
curl -k https://api.fcg-identity.com/health
curl -k https://kong-admin.fcg-identity.com/
```

## 🔒 **SSL/HTTPS via Helm (Recomendado)**

### 1. Instalar cert-manager via Helm (Kubernetes 1.34 Compatível)
```bash
helm install cert-manager jetstack/cert-manager \
  --namespace cert-manager \
  --create-namespace \
  --version v1.15.3 \
  --set crds.enabled=true \
  --set global.leaderElection.namespace=cert-manager
```

### 2. Configurar Let's Encrypt (ClusterIssuer)
```bash
kubectl apply -f - <<EOF
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: your-email@domain.com
    privateKeySecretRef:
      name: letsencrypt-prod
    solvers:
    - http01:
        ingress:
          class: nginx
EOF
```

### 3. Atualizar Ingress para usar SSL
```yaml
spec:
  tls:
  - hosts:
    - keycloak.fcg-identity.com
    secretName: keycloak-tls
  - hosts:
    - kong.fcg-identity.com
    secretName: kong-tls
```

### 4. Verificar Certificados
```bash
kubectl get certificates -A
kubectl describe certificate keycloak-tls -n identity-system
```

## 📊 **Addons Opcionais via Helm**

### 1. Prometheus + Grafana (Monitoramento - K8s 1.34)
```bash
# Adicionar repo
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts

# Instalar stack completo com recursos otimizados
helm install monitoring prometheus-community/kube-prometheus-stack \
  --namespace monitoring \
  --create-namespace \
  --version ^61.0.0 \
  --set grafana.adminPassword=admin123 \
  --set prometheus.prometheusSpec.resources.requests.memory=512Mi \
  --set prometheus.prometheusSpec.resources.limits.memory=1Gi
```

### 2. AWS Load Balancer Controller (K8s 1.34)
```bash
# Adicionar repo
helm repo add eks https://aws.github.io/eks-charts

# Instalar versão compatível com K8s 1.34
helm install aws-load-balancer-controller eks/aws-load-balancer-controller \
  --namespace kube-system \
  --version ^1.8.0 \
  --set clusterName=fcg-identity \
  --set serviceAccount.create=false \
  --set serviceAccount.name=aws-load-balancer-controller \
  --set region=sa-east-1
```

### 3. External DNS (Automatizar Route53 - K8s 1.34)
```bash
helm install external-dns bitnami/external-dns \
  --namespace external-dns \
  --create-namespace \
  --version ^6.28.0 \
  --set provider=aws \
  --set aws.region=sa-east-1 \
  --set txtOwnerId=fcg-identity \
  --set resources.requests.cpu=10m \
  --set resources.requests.memory=32Mi
```

### 4. Listar Addons Instalados
```bash
helm list -A
kubectl get pods -A | grep -E "(ingress|cert-manager|monitoring)"
```

## 📊 **Monitoramento**

### URLs dos Serviços:
- **Keycloak**: https://keycloak.fcg-identity.com
- **Kong Gateway**: https://kong.fcg-identity.com
- **Kong Admin**: https://kong-admin.fcg-identity.com
- **Konga UI**: https://konga.fcg-identity.com
- **Identity API**: https://api.fcg-identity.com

### Comandos Úteis:
```bash
# Ver logs dos pods
kubectl logs -n identity-system -l app=keycloak -f
kubectl logs -n identity-system -l app=kong -f
kubectl logs -n identity-system -l app=identity-api -f

# Escalar deployments
kubectl scale deployment keycloak --replicas=2 -n identity-system
kubectl scale deployment kong --replicas=2 -n identity-system

# Ver recursos
kubectl top pods -n identity-system
kubectl top nodes

# Ver eventos
kubectl get events -n identity-system --sort-by='.lastTimestamp'

# Verificar status completo
kubectl get all -n identity-system

# Port-forward para debug local (se necessário)
kubectl port-forward svc/keycloak 8080:8080 -n identity-system
kubectl port-forward svc/kong-admin 8001:8001 -n identity-system
```

## 🗑️ **Limpeza**

### 1. Remover Aplicação
```bash
# Usando script (recomendado)
./deploy-eks.sh cleanup
# ou
.\deploy-eks.ps1 -Command cleanup

# Manualmente
kubectl delete -f k8s/production/ -n identity-system
kubectl delete namespace identity-system
kubectl get namespaces
```

### 2. Remover Addons Helm
```bash
# Remover addons individuais
helm uninstall ingress-nginx -n ingress-nginx
helm uninstall cert-manager -n cert-manager
helm uninstall monitoring -n monitoring

# Ou remover todos
helm list -A --short | xargs -I {} helm uninstall {}
```

### 3. Remover Namespaces
```bash
kubectl delete namespace ingress-nginx cert-manager monitoring
```

### 4. Remover Cluster EKS
```bash
eksctl delete cluster --name fcg-identity --region sa-east-1 --wait
```

## 🔍 **Verificação de Limpeza Completa**

### 1. Verificar Clusters EKS
```bash
# Verificar se cluster foi removido
aws eks list-clusters --region sa-east-1
eksctl get clusters --region sa-east-1

# Deve retornar lista vazia ou sem fcg-identity
```

### 2. Verificar CloudFormation Stacks
```bash
# Verificar se todos os stacks relacionados foram removidos
aws cloudformation list-stacks --region sa-east-1 --query 'StackSummaries[?contains(StackName,`eksctl-fcg-identity`)].{Name:StackName,Status:StackStatus}' --output table

# Verificar stacks órfãos em DELETE_FAILED
aws cloudformation list-stacks --region sa-east-1 --stack-status-filter DELETE_FAILED --query 'StackSummaries[?contains(StackName,`eksctl-fcg-identity`)].{Name:StackName,Status:StackStatus}' --output table
```

### 3. Verificar VPCs e Recursos de Rede
```bash
# Verificar se VPC foi removido
aws ec2 describe-vpcs --region sa-east-1 --filters "Name=tag:Name,Values=eksctl-fcg-identity-cluster/*" --query 'Vpcs[].{VpcId:VpcId,State:State,Name:Tags[?Key==`Name`].Value|[0]}' --output table

# Verificar subnets órfãs
aws ec2 describe-subnets --region sa-east-1 --filters "Name=tag:Name,Values=*eksctl-fcg-identity*" --query 'Subnets[].{SubnetId:SubnetId,State:State,Name:Tags[?Key==`Name`].Value|[0]}' --output table

# Verificar security groups órfãos
aws ec2 describe-security-groups --region sa-east-1 --filters "Name=tag:Name,Values=*eksctl-fcg-identity*" --query 'SecurityGroups[].{GroupId:GroupId,GroupName:GroupName,Name:Tags[?Key==`Name`].Value|[0]}' --output table
```

### 4. Verificar Internet Gateways e NAT Gateways
```bash
# Verificar Internet Gateways
aws ec2 describe-internet-gateways --region sa-east-1 --filters "Name=tag:Name,Values=*eksctl-fcg-identity*" --query 'InternetGateways[].{InternetGatewayId:InternetGatewayId,State:State,Name:Tags[?Key==`Name`].Value|[0]}' --output table

# Verificar NAT Gateways
aws ec2 describe-nat-gateways --region sa-east-1 --filter "Name=tag:Name,Values=*eksctl-fcg-identity*" --query 'NatGateways[].{NatGatewayId:NatGatewayId,State:State,Name:Tags[?Key==`Name`].Value|[0]}' --output table

# Verificar Elastic IPs órfãos
aws ec2 describe-addresses --region sa-east-1 --filters "Name=tag:Name,Values=*eksctl-fcg-identity*" --query 'Addresses[].{AllocationId:AllocationId,PublicIp:PublicIp,Name:Tags[?Key==`Name`].Value|[0]}' --output table
```

### 5. Verificar Instâncias EC2 e Auto Scaling Groups
```bash
# Verificar instâncias EC2 órfãs
aws ec2 describe-instances --region sa-east-1 --filters "Name=tag:Name,Values=*eksctl-fcg-identity*" "Name=instance-state-name,Values=running,stopped,stopping" --query 'Reservations[].Instances[].{InstanceId:InstanceId,State:State.Name,Name:Tags[?Key==`Name`].Value|[0]}' --output table

# Verificar Auto Scaling Groups
aws autoscaling describe-auto-scaling-groups --region sa-east-1 --query 'AutoScalingGroups[?contains(AutoScalingGroupName,`eksctl-fcg-identity`)].{Name:AutoScalingGroupName,Instances:length(Instances)}' --output table

# Verificar Launch Templates
aws ec2 describe-launch-templates --region sa-east-1 --filters "Name=tag:Name,Values=*eksctl-fcg-identity*" --query 'LaunchTemplates[].{LaunchTemplateId:LaunchTemplateId,LaunchTemplateName:LaunchTemplateName}' --output table
```

### 6. Verificar IAM Roles e Políticas
```bash
# Verificar IAM roles relacionados
aws iam list-roles --query 'Roles[?contains(RoleName,`eksctl-fcg-identity`)].{RoleName:RoleName,CreateDate:CreateDate}' --output table

# Verificar instance profiles
aws iam list-instance-profiles --query 'InstanceProfiles[?contains(InstanceProfileName,`eksctl-fcg-identity`)].{InstanceProfileName:InstanceProfileName,CreateDate:CreateDate}' --output table
```

### 7. Verificar Load Balancers (se houver)
```bash
# Verificar Network Load Balancers
aws elbv2 describe-load-balancers --region sa-east-1 --query 'LoadBalancers[?contains(LoadBalancerName,`fcg-identity`) || contains(to_string(Tags),`fcg-identity`)].{LoadBalancerName:LoadBalancerName,State:State.Code}' --output table

# Verificar Classic Load Balancers
aws elb describe-load-balancers --region sa-east-1 --query 'LoadBalancerDescriptions[?contains(LoadBalancerName,`fcg-identity`)].{LoadBalancerName:LoadBalancerName,Scheme:Scheme}' --output table
```

### 8. Script de Verificação Completa
```bash
#!/bin/bash
echo "🔍 VERIFICAÇÃO COMPLETA DE LIMPEZA - FCG-IDENTITY"
echo "================================================"

echo -e "\n1️⃣  Clusters EKS:"
aws eks list-clusters --region sa-east-1 --query 'clusters' --output text

echo -e "\n2️⃣  CloudFormation Stacks:"
aws cloudformation list-stacks --region sa-east-1 --query 'StackSummaries[?contains(StackName,`eksctl-fcg-identity`)].{Name:StackName,Status:StackStatus}' --output table

echo -e "\n3️⃣  VPCs:"
aws ec2 describe-vpcs --region sa-east-1 --filters "Name=tag:Name,Values=*eksctl-fcg-identity*" --query 'Vpcs[].VpcId' --output text

echo -e "\n4️⃣  Instâncias EC2:"
aws ec2 describe-instances --region sa-east-1 --filters "Name=tag:Name,Values=*eksctl-fcg-identity*" "Name=instance-state-name,Values=running,stopped,stopping" --query 'Reservations[].Instances[].InstanceId' --output text

echo -e "\n5️⃣  IAM Roles:"
aws iam list-roles --query 'Roles[?contains(RoleName,`eksctl-fcg-identity`)].RoleName' --output text

echo -e "\n✅ Verificação concluída! Se todas as seções estiverem vazias, a limpeza foi bem-sucedida."
```

## 💰 **Custos Estimados (sa-east-1)**

### **Configuração Recomendada (m7i-flex.large + Amazon Linux)**  
- **EKS Cluster**: ~$73/mês
- **EC2 Instances** (3x m7i-flex.large): ~$162/mês
- **Load Balancer (NLB)**: ~$18/mês
- **EBS Storage** (90GB): ~$9/mês
- **Data Transfer**: ~$10/mês

**Total recomendado**: ~$272/mês ✅ **AWS Nativo Otimizado**

### **Configuração Futura (t3.large) - Após Aumento de Quota**  
- **EKS Cluster**: ~$73/mês
- **EC2 Instances** (3x t3.large): ~$270/mês
- **Load Balancer (NLB)**: ~$18/mês
- **EBS Storage** (90GB): ~$9/mês
- **Data Transfer**: ~$10/mês

**Total futuro**: ~$380/mês

### **Alta Performance (c5.xlarge)**
- **EKS Cluster**: ~$73/mês
- **EC2 Instances** (3x c5.xlarge): ~$540/mês
- **Load Balancer (NLB)**: ~$18/mês
- **EBS Storage** (150GB): ~$15/mês
- **Data Transfer**: ~$20/mês

**Total alta performance**: ~$666/mês

### **Recursos por Instância:**

#### **c5.xlarge (Produção)**
- **vCPUs**: 4
- **RAM**: 8 GB
- **Performance**: Alta CPU, otimizada para compute
- **Ideal para**: Keycloak, Kong, APIs com carga

#### **m7i-flex.large (Recomendado)**
- **vCPUs**: 2 (Intel Xeon 4th Gen)
- **RAM**: 8 GB
- **Performance**: CPU baseline de 50% com burst até 100%
- **Ideal para**: Produção estável, workloads variáveis
- **Vantagens**: Flexibilidade de CPU + custo otimizado
- **SO**: Amazon Linux 2023 (otimizado para AWS/EKS)

#### **t3.large (Alternativa)**
- **vCPUs**: 2 (burstable até 3.6 GHz)  
- **RAM**: 8 GB
- **Performance**: Excelente custo-benefício com burst
- **Ideal para**: Desenvolvimento, POCs
- **Burst**: 2880 CPU credits por hora

## 🎛️ **Dimensionamento dos Serviços**

### **Distribuição de Recursos por Pod:**

```yaml
# Keycloak (Alto consumo de CPU/RAM)
resources:
  requests:
    cpu: 500m
    memory: 1Gi
  limits:
    cpu: 1000m
    memory: 2Gi

# Kong Gateway (Médio consumo, alta rede)
resources:
  requests:
    cpu: 250m
    memory: 512Mi
  limits:
    cpu: 500m
    memory: 1Gi

# PostgreSQL Databases (Médio consumo, I/O intensivo)
resources:
  requests:
    cpu: 250m
    memory: 512Mi
  limits:
    cpu: 500m
    memory: 1Gi

# Identity API (.NET - Otimizada)
resources:
  requests:
    cpu: 100m
    memory: 256Mi
  limits:
    cpu: 200m
    memory: 512Mi
```

### **Cálculo Total por Node (m7i-flex.large):**
- **Keycloak**: 0.5 CPU, 1.5GB RAM
- **Kong**: 0.3 CPU, 0.5GB RAM  
- **2x PostgreSQL**: 0.5 CPU, 1GB RAM
- **Identity API**: 0.1 CPU, 0.3GB RAM
- **Konga + Sistema**: 0.1 CPU, 0.2GB RAM

**Total por node**: ~1.5 CPU, 3.5GB RAM (utilização ~75% CPU, ~44% RAM)

� **Vantagens m7i-flex.large + Ubuntu**:
- **CPU Flexível**: Baseline de 50% com burst até 100% quando necessário
- **Intel 4th Gen**: Processadores mais eficientes e rápidos
- **Ubuntu 20.04**: Melhor compatibilidade com Kubernetes e containers
- **Custo Otimizado**: Paga apenas pelo que usa (flex pricing)
- **Escalabilidade**: Fácil distribuição de carga entre nodes

## 🔧 **Troubleshooting**

### Problemas Comuns:

1. **Pods não iniciam**:
   ```bash
   kubectl describe pod <pod-name> -n identity-system
   ```

2. **Ingress sem IP externo**:
   ```bash
   kubectl get svc -n ingress-nginx
   # Verifique se LoadBalancer tem EXTERNAL-IP
   ```

3. **DNS não resolve**:
   ```bash
   nslookup keycloak.fcg-identity.com
   ```

4. **Permissões ECR**:
   ```bash
   aws ecr describe-repositories --region sa-east-1
   ```

## 🔍 **Verificação Final - Kubernetes 1.34**

### 1. Verificar Cluster e Addons
```bash
# Verificar versão do cluster
kubectl version --short

# Verificar addons instalados
eksctl get addons --cluster fcg-identity --region sa-east-1

# Verificar nodes
kubectl get nodes -o wide
```

### 2. Verificar Helm Charts
```bash
# Listar releases do Helm
helm list --all-namespaces

# Verificar status do nginx-ingress
kubectl get pods -n ingress-nginx
kubectl get svc -n ingress-nginx

# Verificar cert-manager
kubectl get pods -n cert-manager
```

### 3. Verificar Aplicações
```bash
# Verificar pods da aplicação
kubectl get pods -n identity-system

# Verificar ingress
kubectl get ingress -n identity-system

# Verificar logs se necessário
kubectl logs -f deployment/identity-api -n identity-system
```

### 4. Teste de Conectividade Final
```bash
# Testar endpoints principais
curl -k https://identity-api.fcg-identity.com/health
curl -k https://keycloak.fcg-identity.com/realms/master
curl -k https://kong-admin.fcg-identity.com/
```

---

## � **Troubleshooting - Timeouts na Criação do Cluster**

### Problemas Comuns e Soluções:

#### 1. **Timeout na Criação (exceeded max wait time)**
```bash
# Verificar se há recursos órfãos de tentativas anteriores
aws cloudformation list-stacks --region sa-east-1 --query 'StackSummaries[?contains(StackName,`eksctl-fcg-identity`)]'

# Limpar stacks órfãos
aws cloudformation delete-stack --stack-name eksctl-fcg-identity-cluster --region sa-east-1
aws cloudformation delete-stack --stack-name eksctl-fcg-identity-nodegroup-standard-workers --region sa-east-1
aws cloudformation delete-stack --stack-name eksctl-fcg-identity-nodegroup-workers --region sa-east-1
```

#### 1a. **Configuração Produção (m7i-flex.large + Ubuntu)**
```bash
# � SOLUÇÃO RECOMENDADA: m7i-flex.large com Ubuntu
# Instâncias Intel flexíveis com melhor custo-benefício

# Verificar disponibilidade do tipo de instância
aws ec2 describe-instance-type-offerings --location-type availability-zone --filters Name=instance-type,Values=m7i-flex.large --region sa-east-1

# Criar nodegroup com Ubuntu (recomendado para produção)
eksctl create nodegroup \
  --cluster fcg-identity \
  --region sa-east-1 \
  --name ubuntu-workers \
  --node-type m7i-flex.large \
  --nodes 3 \
  --nodes-min 2 \
  --nodes-max 5 \
  --managed \
  --node-ami-family Ubuntu2004 \
  --full-ecr-access

# Alternativa com t3.micro para Free Tier (se necessário)
eksctl create nodegroup \
  --cluster fcg-identity \
  --region sa-east-1 \
  --name micro-workers \
  --node-type t3.micro \
  --nodes 2 \
  --nodes-min 1 \
  --nodes-max 3 \
  --managed \
  --full-ecr-access
```

#### 2. **Quotas da AWS Insuficientes**
```bash
# Verificar quota de clusters EKS (padrão: 100) ✅ Sua quota: 100
aws service-quotas get-service-quota --service-code eks --quota-code L-1194D53C --region sa-east-1

# Verificar quota de instâncias EC2 ⚠️ Sua quota atual: 16 vCPUs
aws service-quotas get-service-quota --service-code ec2 --quota-code L-1216C47A --region sa-east-1

# 🚨 IMPORTANTE: Solicitar aumento de vCPUs para produção (recomendado: 64 vCPUs)
aws service-quotas request-service-quota-increase \
  --service-code ec2 \
  --quota-code L-1216C47A \
  --desired-value 64 \
  --region sa-east-1

# Verificar status da solicitação
aws service-quotas list-requested-service-quota-change-history --service-code ec2 --region sa-east-1
```

**Distribuição de vCPUs por Tipo de Instância:**
- **m7i-flex.large**: 2 vCPUs (total: 6-10 vCPUs para 3-5 nodes) ✅ **Recomendado**
- **t3.micro**: 2 vCPUs (total: 2-6 vCPUs para 1-3 nodes) - Free Tier
- **t3.small**: 2 vCPUs (total: 4-6 vCPUs para 2-3 nodes)  
- **t3.medium**: 2 vCPUs (total: 6-8 vCPUs para 3-4 nodes)
- **t3.large**: 2 vCPUs (total: 4-6 vCPUs para 2-3 nodes)
- **t3.xlarge**: 4 vCPUs (total: 8-16 vCPUs para 2-4 nodes)
```

#### 3. **Problemas de Rede/VPC**
```bash
# Verificar se as zonas suportam t3.large
aws ec2 describe-instance-type-offerings --location-type availability-zone --filters Name=instance-type,Values=t3.large --region sa-east-1

# Verificar limites de VPC
aws ec2 describe-account-attributes --attribute-names supported-platforms --region sa-east-1
```

#### 4. **Configuração Alternativa Mais Confiável**
```bash
# Configuração robusta com Amazon Linux (recomendado)
eksctl create cluster \
  --name fcg-identity \
  --region sa-east-1 \
  --version 1.34 \
  --node-type m7i-flex.large \
  --nodes 3 \
  --nodes-min 2 \
  --nodes-max 5 \
  --managed \
  --zones sa-east-1a,sa-east-1b \
  --timeout=60m \
  --verbose 4

# Alternativa com instâncias menores (Free Tier)
eksctl create cluster \
  --name fcg-identity-test \
  --region sa-east-1 \
  --version 1.34 \
  --node-type t3.micro \
  --nodes 2 \
  --nodes-min 1 \
  --nodes-max 3 \
  --managed \
  --zones sa-east-1a \
  --timeout=30m
```

#### 5. **Verificar Status em Tempo Real**
```bash
# Monitorar criação do cluster
watch -n 30 'eksctl get cluster fcg-identity --region sa-east-1'

# Monitorar CloudFormation
aws cloudformation describe-stacks --stack-name eksctl-fcg-identity-cluster --region sa-east-1 --query 'Stacks[0].StackStatus'

# Verificar eventos do CloudFormation
aws cloudformation describe-stack-events --stack-name eksctl-fcg-identity-cluster --region sa-east-1 --query 'StackEvents[0:5].[Timestamp,LogicalResourceId,ResourceStatus,ResourceStatusReason]' --output table
```

---

## �📋 **Resumo da Configuração**

- ✅ **Cluster EKS**: Kubernetes 1.34 com m7i-flex.large nodes (Amazon Linux)
- ✅ **Addons**: EBS CSI v1.49.0, VPC CNI v1.20.3 
- ✅ **Ingress**: Nginx v4.11+ com Network Load Balancer
- ✅ **SSL**: Cert-manager v1.15.3 com Let's Encrypt
- ✅ **Monitoramento**: Prometheus Stack v61.0+
- ✅ **DNS**: External DNS v6.28+ para Route53
- ✅ **Região**: sa-east-1 (São Paulo)
- ✅ **Domínios**: *.fcg-identity.com prontos para produção