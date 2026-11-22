terraform {
  required_version = ">= 1.6"
  required_providers {
    azurerm = { source = "hashicorp/azurerm", version = "~> 4.0" }
  }
  backend "azurerm" {                      # remote state with locking
    resource_group_name  = "rg-tfstate"
    storage_account_name = "sttfstatebanking"
    container_name       = "tfstate"
    key                  = "aks/prod.tfstate"
    use_azuread_auth     = true
  }
}

provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "rg" {
  name     = "rg-${var.prefix}-${var.environment}"
  location = var.location
  tags     = local.tags
}

resource "azurerm_container_registry" "acr" {
  name                = replace("acr${var.prefix}${var.environment}", "-", "")
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku                 = "Premium"
  admin_enabled       = false
  tags                = local.tags
}

resource "azurerm_kubernetes_cluster" "aks" {
  name                      = "aks-${var.prefix}-${var.environment}"
  resource_group_name       = azurerm_resource_group.rg.name
  location                  = azurerm_resource_group.rg.location
  dns_prefix                = "${var.prefix}-${var.environment}"
  oidc_issuer_enabled       = true            # required for workload identity
  workload_identity_enabled = true
  tags                      = local.tags

  default_node_pool {
    name                 = "system"
    vm_size              = var.node_size
    auto_scaling_enabled = true
    min_count            = 2
    max_count            = 5
    zones                = ["1", "2", "3"]
  }

  identity { type = "SystemAssigned" }

  network_profile {
    network_plugin      = "azure"
    network_plugin_mode = "overlay"
    network_policy      = "azure"
  }
}

# Let AKS pull from ACR without passwords
resource "azurerm_role_assignment" "aks_acr_pull" {
  scope                            = azurerm_container_registry.acr.id
  role_definition_name             = "AcrPull"
  principal_id                     = azurerm_kubernetes_cluster.aks.kubelet_identity[0].object_id
  skip_service_principal_aad_check = true
}

locals {
  tags = { app = var.prefix, env = var.environment, owner = "platform-team" }
}
