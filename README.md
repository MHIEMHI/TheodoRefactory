### Consignes .NET: 
* Ignorez les migrations BDD
* Ne pas modifier les classes qui ont un commentaire: `// WARN: Should not be changed during the exercise
`
* Pour lancer les tests: `dotnet test`

### Architecture
* `Refacto.DotNet.Domain`: entités et types de domaine indépendants de l'infrastructure.
* `Refacto.DotNet.Application`: contrats et cas d'utilisation d'orchestration et de règles produit.
* `Refacto.DotNet.Infrastructure`: persistance Entity Framework et adaptateurs techniques.
* `Refacto.DotNet.Controllers`: API HTTP et composition des dépendances.

Les dépendances vont vers le cœur: l'API utilise l'application, l'infrastructure implémente les contrats de l'application, et l'application dépend du domaine.