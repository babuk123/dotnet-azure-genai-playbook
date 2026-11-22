output "aks_name"          { value = azurerm_kubernetes_cluster.aks.name }
output "acr_login_server"  { value = azurerm_container_registry.acr.login_server }
output "oidc_issuer_url"   { value = azurerm_kubernetes_cluster.aks.oidc_issuer_url }
