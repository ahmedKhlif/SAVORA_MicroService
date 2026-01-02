# SAVORA - Smart After-Sales Service

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)
![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?style=flat-square&logo=blazor)
![SQLite](https://img.shields.io/badge/SQLite-3-003B57?style=flat-square&logo=sqlite)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat-square&logo=docker)
![License](https://img.shields.io/badge/License-MIT-green.svg)

> Application de gestion de service après-vente (SAV) construite avec une architecture de microservices moderne utilisant .NET 8 et Blazor WebAssembly.

---

## 📋 Table des matières

- [Fonctionnalités principales](#-fonctionnalités-principales)
- [Architecture](#-architecture)
- [Technologies utilisées](#️-technologies-utilisées)
- [Installation et démarrage](#-installation-et-démarrage)
- [Structure du projet](#-structure-du-projet)
- [Sécurité](#-sécurité)
- [Documentation API](#-documentation-api)
- [Contribution](#-contribution)

---

## 🚀 Fonctionnalités principales

### ✅ Gestion des réclamations
- Création et suivi des réclamations clients
- Gestion des statuts et priorités (Basse, Moyenne, Haute, Urgente)
- Historique complet des modifications
- Gestion des pièces jointes
- Calcul automatique des délais SLA
- Notifications en temps réel

### 🔧 Gestion des interventions
- Planification et assignation des interventions aux techniciens
- Suivi en temps réel (Planifiée, En cours, Terminée, Annulée)
- Gestion des pièces utilisées et de la main d'œuvre
- Génération automatique des factures PDF
- Gestion du stock des pièces détachées (déduction/restauration automatique)
- Calcul automatique des coûts

### 📦 Gestion des articles
- Enregistrement des articles clients
- Suivi de la garantie avec calcul automatique
- Association des articles aux clients
- Historique des achats
- Catégorisation des articles

### 🔩 Gestion des pièces détachées
- Catalogue complet des pièces détachées
- Gestion du stock (quantités, alertes de stock bas)
- Suivi des mouvements de stock
- Catégorisation des pièces
- Suggestions automatiques (référence, catégorie)

### 🔔 Notifications et messages
- Notifications en temps réel pour les changements de statut
- Système de messagerie entre utilisateurs
- Compteur de notifications non lues
- Historique complet des messages
- Suggestions de sujets pour les messages

### 📊 Tableaux de bord
- Dashboard SAV avec métriques clés (KPIs)
- Dashboard client personnalisé
- Graphiques interactifs (Chart.js)
- Export Excel et PDF
- Statistiques détaillées

### 🔐 Authentification et autorisation
- Authentification JWT avec tokens
- Gestion des rôles (ResponsableSAV, Client, Technicien)
- Profils utilisateurs avec photos de profil
- Sécurité renforcée
- Gestion des sessions

### 🎨 Interface utilisateur
- Design moderne et responsive avec AdminKit
- Mode sombre/clair avec sauvegarde de préférence
- Interface Blazor WebAssembly
- Navigation intuitive
- Composants MudBlazor

---

## 🏗️ Architecture

Le projet utilise une **architecture de microservices** avec séparation des responsabilités :

```
┌─────────────────────────────────────────────────────────────┐
│                    SAVORA Frontend                          │
│              (Blazor WebAssembly + MudBlazor)               │
│                      Port: 5000                             │
└───────────────────────────┬─────────────────────────────────┘
                            │
          ┌─────────────────┼─────────────────┐
          │                 │                 │
          ▼                 ▼                 ▼
┌─────────────┐   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐
│    Auth     │   │  Articles   │   │ Reclamations│   │Interventions│
│   Service   │   │   Service   │   │   Service   │   │   Service   │
│   Port:5001 │   │  Port:5002  │   │  Port:5003  │   │  Port:5004  │
└──────┬──────┘   └──────┬──────┘   └──────┬──────┘   └──────┬──────┘
       │                 │                 │                 │
       ▼                 ▼                 ▼                 ▼
┌─────────────┐   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐
│ SQLite DB   │   │ SQLite DB   │   │ SQLite DB   │   │ SQLite DB   │
└─────────────┘   └─────────────┘   └─────────────┘   └─────────────┘
```

### Services Backend

| Service | Port | Description | Base de données |
|---------|------|-------------|-----------------|
| **AuthService** | 5001 | Authentification JWT, gestion utilisateurs, photos de profil | `savora_auth.db` |
| **ArticlesService** | 5002 | Articles clients, pièces détachées, gestion du stock | `savora_articles.db` |
| **ReclamationsService** | 5003 | Réclamations clients, notifications, messages, clients, dashboard | `savora_reclamations.db` |
| **InterventionsService** | 5004 | Interventions techniques, techniciens, factures PDF | `savora_interventions.db` |
| **ApiGateway** | 5010 | Point d'entrée unique, routage, authentification centralisée | - |
| **Frontend** | 5000 | Application Blazor WebAssembly | - |

### Communication inter-services

Les microservices communiquent entre eux via des appels HTTP avec transmission des tokens JWT pour l'authentification. L'API Gateway sert de point d'entrée unique pour le frontend.

---

## 🛠️ Technologies utilisées

### Backend
- **.NET 8.0** - Framework principal
- **ASP.NET Core Web API** - Framework web
- **Entity Framework Core 8.0** - ORM
- **SQLite** - Base de données (fichier local)
- **JWT Authentication** - Authentification et autorisation
- **Serilog** - Logging
- **Swagger/OpenAPI** - Documentation API

### Frontend
- **Blazor WebAssembly** - Framework UI
- **MudBlazor 6.11** - Bibliothèque de composants UI
- **AdminKit** - Thème Bootstrap professionnel
- **Chart.js** - Graphiques interactifs
- **Blazored.LocalStorage** - Stockage local
- **Feather Icons** - Icônes

### Infrastructure
- **API Gateway (Ocelot)** - Routage et gestion des requêtes
- **Docker** (Optionnel) - Conteneurisation
- **SQLite** - Base de données légère et portable

---

## 📋 Prérequis

Avant de commencer, assurez-vous d'avoir installé :

- **.NET 8.0 SDK** ou supérieur ([Télécharger](https://dotnet.microsoft.com/download))
- **Git** ([Télécharger](https://git-scm.com/downloads))
- **Visual Studio 2022** / **VS Code** / **Rider** (optionnel, pour l'IDE)
- **Docker Desktop** (optionnel, pour la conteneurisation)

---

## 🚀 Installation et démarrage

### 1. Cloner le repository

```bash
git clone https://github.com/ahmedKhlif/SAVORA_MicroService-.git
cd SAVORA_MicroService-
```

### 2. Configuration des ports

Les services sont configurés pour fonctionner avec les ports par défaut :

| Service | URL |
|---------|-----|
| Frontend | `http://localhost:5000` |
| ApiGateway | `http://localhost:5010` |
| AuthService | `http://localhost:5001` |
| ArticlesService | `http://localhost:5002` |
| ReclamationsService | `http://localhost:5003` |
| InterventionsService | `http://localhost:5004` |

### 3. Démarrer les services

#### Option 1 : Démarrage manuel (Recommandé pour le développement)

Ouvrez un terminal pour chaque service dans l'ordre suivant :

**Terminal 1 - AuthService:**
```bash
cd src/services/AuthService
dotnet run
```

**Terminal 2 - ArticlesService:**
```bash
cd src/services/ArticlesService
dotnet run
```

**Terminal 3 - ReclamationsService:**
```bash
cd src/services/ReclamationsService
dotnet run
```

**Terminal 4 - InterventionsService:**
```bash
cd src/services/InterventionsService
dotnet run
```

**Terminal 5 - ApiGateway:**
```bash
cd src/services/ApiGateway
dotnet run
```

**Terminal 6 - Frontend:**
```bash
cd src/frontend/Savora.BlazorWasm
dotnet run
```

#### Option 2 : Script PowerShell (Windows)

Utilisez le script fourni dans `src/run-local.ps1` pour démarrer tous les services :

```powershell
cd src
.\run-local.ps1
```

#### Option 3 : Docker Compose (Optionnel)

Si vous préférez utiliser Docker :

```bash
docker-compose up -d
```

### 4. Accéder à l'application

Une fois tous les services démarrés, ouvrez votre navigateur et accédez à :

```
http://localhost:5000
```

### 5. Comptes par défaut

L'application inclut des données de seed avec des comptes de test :

**Responsable SAV:**
- **Email:** `admin@savora.com`
- **Password:** `Admin@123`

**Client:**
- **Email:** `client@savora.com`
- **Password:** `Client@123`

---

## 📁 Structure du projet

```
SAVORA_MicroService-/
├── src/
│   ├── gateway/
│   │   └── ApiGateway/              # API Gateway (Ocelot)
│   │       ├── Program.cs
│   │       ├── appsettings.json
│   │       └── ocelot.json
│   │
│   ├── services/
│   │   ├── AuthService/             # Service d'authentification
│   │   │   ├── Controllers/
│   │   │   ├── Application/
│   │   │   ├── Domain/
│   │   │   ├── Infrastructure/
│   │   │   └── Program.cs
│   │   │
│   │   ├── ArticlesService/         # Service de gestion des articles
│   │   │   ├── Controllers/
│   │   │   ├── Application/
│   │   │   ├── Domain/
│   │   │   ├── Infrastructure/
│   │   │   └── Program.cs
│   │   │
│   │   ├── ReclamationsService/     # Service de gestion des réclamations
│   │   │   ├── Controllers/
│   │   │   ├── Application/
│   │   │   ├── Domain/
│   │   │   ├── Infrastructure/
│   │   │   └── Program.cs
│   │   │
│   │   └── InterventionsService/    # Service de gestion des interventions
│   │       ├── Controllers/
│   │       ├── Application/
│   │       ├── Domain/
│   │       ├── Infrastructure/
│   │       └── Program.cs
│   │
│   ├── frontend/
│   │   └── Savora.BlazorWasm/       # Application Blazor WebAssembly
│   │       ├── Pages/
│   │       ├── Services/
│   │       ├── Shared/
│   │       ├── wwwroot/
│   │       └── Program.cs
│   │
│   └── shared/
│       └── Savora.Shared/           # DTOs et modèles partagés
│           ├── DTOs/
│           └── Enums/
│
├── docker-compose.yml               # Configuration Docker
├── init-databases.sh                # Script d'initialisation (PostgreSQL)
├── README.md                        # Documentation
└── .gitignore
```

### Structure d'un microservice

Chaque microservice suit une architecture Clean Architecture :

```
ServiceName/
├── Controllers/          # API Controllers (endpoints HTTP)
├── Application/
│   └── Services/        # Services applicatifs (logique métier)
├── Domain/
│   └── Entities/        # Entités métier
├── Infrastructure/
│   └── Data/            # DbContext, Seeders, Migrations
├── Program.cs           # Point d'entrée et configuration
├── appsettings.json     # Configuration
└── Dockerfile           # Image Docker (optionnel)
```

---

## 🔐 Sécurité

### Authentification
- **JWT (JSON Web Tokens)** pour l'authentification
- Tokens avec expiration configurable
- Transmission sécurisée via headers HTTP

### Autorisation
- **RBAC (Role-Based Access Control)** 
- Rôles supportés : `ResponsableSAV`, `Client`, `Technicien`
- Endpoints protégés avec `[Authorize(Roles = "...")]`

### Sécurité des données
- **BCrypt** pour le hachage des mots de passe
- Validation des entrées utilisateur
- Protection CORS configurée
- Transmission sécurisée des tokens entre microservices via `IHttpContextAccessor`

---

## 📊 Fonctionnalités avancées

### Dashboard SAV
- Métriques en temps réel (KPIs)
- Graphiques interactifs avec Chart.js
- Export Excel (CSV) et PDF
- Filtres et recherches avancées
- Statistiques détaillées par période

### Gestion du stock
- Déduction automatique lors de l'utilisation des pièces
- Restauration du stock lors de la suppression
- Alertes de stock bas
- Historique complet des mouvements
- Contrôle des quantités disponibles

### Notifications
- Notifications en temps réel
- Compteur de notifications non lues
- Historique complet
- Notifications pour changements de statut, assignations, etc.
- Marquer comme lu / tout marquer comme lu

### Mode sombre
- Toggle mode sombre/clair dans la navbar
- Sauvegarde de la préférence dans le localStorage
- Support complet de tous les composants
- Transitions fluides

### Autres fonctionnalités
- Suggestions automatiques (références, catégories, compétences, sujets)
- Génération de factures PDF
- Gestion des garanties avec calcul automatique
- SLA (Service Level Agreement) avec délais par priorité
- Historique complet des modifications

---

## 📝 Documentation API

Chaque microservice expose sa documentation Swagger :

- **Auth Service:** http://localhost:5001/swagger
- **Articles Service:** http://localhost:5002/swagger
- **Reclamations Service:** http://localhost:5003/swagger
- **Interventions Service:** http://localhost:5004/swagger

L'API Gateway centralise également la documentation : http://localhost:5010/swagger

---

## 🧪 Tests

Pour exécuter les tests (si disponibles) :

```bash
cd src
dotnet test
```

---

## 📝 Configuration

### Variables d'environnement

Les services utilisent `appsettings.json` pour la configuration. Les principales configurations incluent :

- **Connection strings** (SQLite) : `Data Source=Data/savora_*.db`
- **JWT settings** : SecretKey, Issuer, Audience, Expiration
- **URLs des services** : Pour la communication inter-services
- **Configuration de l'API Gateway** : Routage vers les microservices

### Configuration du frontend

Le frontend se connecte aux services via l'API Gateway. La configuration se trouve dans :

```
src/frontend/Savora.BlazorWasm/wwwroot/appsettings.json
```

---

## 🤝 Contribution

Les contributions sont les bienvenues ! Pour contribuer :

1. **Fork** le projet
2. Créez une branche pour votre fonctionnalité (`git checkout -b feature/AmazingFeature`)
3. **Commit** vos changements (`git commit -m 'Add some AmazingFeature'`)
4. **Push** vers la branche (`git push origin feature/AmazingFeature`)
5. Ouvrez une **Pull Request**

### Guidelines de contribution

- Suivez les conventions de code C# existantes
- Ajoutez des commentaires pour le code complexe
- Testez vos modifications
- Mettez à jour la documentation si nécessaire

---

## 📄 Licence

Ce projet est sous licence **MIT**. Voir le fichier `LICENSE` pour plus de détails.

---



---

## 🙏 Remerciements

- [AdminKit](https://adminkit.io/) pour le thème UI professionnel
- [MudBlazor](https://mudblazor.com/) pour les composants Blazor
- La communauté .NET et Blazor pour le support
- Tous les contributeurs open-source qui ont rendu ce projet possible

---

## 📞 Support

Pour toute question, problème ou suggestion :

- Ouvrez une [issue](https://github.com/ahmedKhlif/SAVORA_MicroService-/issues) sur GitHub
- Contactez l'équipe de développement

---

<div align="center">

**SAVORA** - Smart After-Sales Service, Simplified. 🚀

Made with ❤️ using .NET 8 and Blazor

</div>
