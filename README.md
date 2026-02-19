# 📋 Documentation du Jeu - Guide pour Développeur Front-End

## Vue d'ensemble

Ce jeu est un jeu de plateau tour par tour où 2-4 joueurs placent de la lave sur une grille pour éliminer les adversaires en recouvrant leurs bases.

---

## 🎯 Structure des Scripts Principaux

### **1. GridManager.cs** - Gestion de la Grille
**Rôle :** Crée et gère la grille de jeu (damier 16x16 par défaut)

**Fonctionnalités importantes :**
- `GenerateGrid()` : Crée tous les tiles numérotés
- `GetTileByNumber(int)` : Récupère un tile par son numéro
- `GetTileByCoordinates(x, y)` : Récupère un tile par position

**Dictionnaires clés :**
- `tiles` : Numéro → GameObject
- `tilesByCoordinates` : Position → GameObject

**À savoir pour le front-end :** Chaque tile a un composant `TileData` qui stocke son numéro et ses coordonnées.

---

### **2. GameManager.cs** - Logique Centrale du Jeu
**Rôle :** Gère toute la logique du jeu (tours, placement, victoire)

#### **Collections importantes :**
- `occupiedTiles` - Tiles avec quelque chose dessus (lave, bloque)
- `lavaTiles` - Tiles avec de la lave uniquement
- `blockTiles` - Tiles avec des bloques
- `initialLavaTiles` - Les 4 laves du centre (protégées)
- `playerBases` - Tiles des bases des joueurs
- `eliminatedPlayers` - Joueurs éliminés

#### **Système de Tours :**
1. **Phase 1 :** `PlaceMandatoryLava()` - Placer 1 lave selon la direction du vent (obligatoire)
2. **Phase 2 :** `PlayCardFromHand()` - Jouer 1 carte (obligatoire)
3. **Fin :** `EndTurn()` → `NextTurn()` - Passer au joueur suivant

#### **Méthodes clés pour l'UI :**
- `GetCurrentPlayer()` - Retourne le joueur actuel
- `GetCurrentWind()` - Retourne la direction du vent
- `IsTileOccupied(int)` - Vérifie si un tile est occupé
- `IsTileLava(int)` / `IsTileBlock(int)` - Type de pièce

---

### **3. CardSystem.cs** - Définition des Cartes
**Rôle :** Définit les types de cartes et leurs patterns

#### **Types de cartes :**
- `CardType.Lava` - Place de la lave
- `CardType.Water` - Enlève la lave
- `CardType.Block` - Place des bloques (obstacles)
- `CardType.WindDirection` - Change la direction du vent

#### **Patterns disponibles :**
- `Line3` - Ligne de 3 (rotation possible)
- `Square2x2` - Carré 2x2
- `TwoAdjacent` - 2 bloques côte à côte
- `OneSpaceOne` - 2 bloques avec 1 case vide entre

---

### **4. PlayerHand.cs** - Main du Joueur
**Rôle :** Gère les cartes disponibles pour chaque joueur

**Important :** Pour l'instant, tous les joueurs ont accès à **toutes les cartes en permanence** (pas de pioche/défausse).

**Méthode clé :**
- `InitializeAllCards()` - Donne les 10 cartes au joueur
- Liste complète : 2 Laves, 2 Eaux, 2 Bloques, 4 Vents

---

### **5. CardPlacementSystem.cs** - Placement Visuel des Cartes
**Rôle :** Gère le preview et la validation du placement des cartes

#### **Flow de placement :**
1. `StartPlacingCard(index)` - Démarre le mode placement
2. `UpdatePreview()` - Affiche les previews (vert = valide, rouge = invalide)
3. `ConfirmPlacement()` - Valide et place la carte
4. `CancelPlacement()` - Annule

#### **Validations importantes :**
- **Lave :** Doit toucher une lave existante, toutes les tiles doivent être connectées (pas de diagonale)
- **Eau :** Peut être placée n'importe où (sauf sur les 4 laves initiales protégées)
- **Bloque :** Ne peut pas être placé sur une case occupée
- **Pattern hors grille :** Autorisé (ex: 2x2 dans un coin avec 2 cases hors grille)

---

### **6. GameTester.cs** - Contrôles Clavier (Temporaire)
**Rôle :** Permet de tester le jeu sans UI complète

**Contrôles actuels :**
- Clic gauche : Placer lave obligatoire OU confirmer carte
- Touches 1-0 : Sélectionner une carte
- Q : Pivoter le pattern
- Clic droit : Annuler
- R : Reset
- H : Aide (console)

**⚠️ À remplacer par une vraie UI tactile/souris**

---

## 🎨 Points d'Intégration Front-End

### **1. Affichage de l'État du Jeu**

**Informations à afficher :**

Joueur actuel :
- `int currentPlayerNumber = gameManager.GetCurrentPlayer().playerNumber;`

Direction du vent :
- `WindDirection wind = gameManager.GetCurrentWind();`

Cartes disponibles :
- `PlayerHand hand = gameManager.GetCurrentPlayerHand();`
- Boucle sur `hand.GetHandSize()` pour récupérer chaque carte
- Utilise `card.cardName` et `card.type` pour l'affichage

### **2. Highlight Visuel**

La base du joueur actuel s'illumine automatiquement via `HighlightCurrentPlayerBase()` (appelé à chaque `NextTurn()`)

### **3. Preview des Cartes**

Le système de preview existe déjà avec des cubes verts/rouges. Pour une UI plus jolie :
- Utilise `CardPlacementSystem.selectedTiles` pour savoir quelles cases sont sélectionnées
- Utilise `ValidatePlacement()` pour savoir si c'est valide
- Crée tes propres effets visuels (particules, outline, etc.)

### **4. Notifications/Feedback**

**Événements importants à afficher :**
- Joueur éliminé : Détecté dans `CheckBaseElimination()`
- Victoire : Détecté dans `CheckGameOver()`
- Action invalide : Messages via `Debug.LogWarning()`

**Suggestion :** Remplace les `Debug.Log()` par des events C# que l'UI peut écouter.

---

## 🔧 Modifications Recommandées pour le Front-End

### **1. Système d'Events**
Ajoute des UnityEvents pour communiquer avec l'UI :
- `OnPlayerChanged` - Quand le joueur change
- `OnWindChanged` - Quand la direction du vent change
- `OnPlayerEliminated` - Quand un joueur est éliminé
- `OnGameWon` - Quand la partie est gagnée

### **2. Remplacer GameTester**
Crée une vraie UI avec :
- Boutons pour sélectionner les cartes (au lieu de touches 1-0)
- Bouton de rotation (au lieu de Q)
- Boutons Confirmer/Annuler (au lieu de clics)
- Indicateur visuel de phase (Phase 1 / Phase 2)

### **3. Animations**
Ajoute des animations pour :
- Placement de pièces (scale-in, particules)
- Élimination de joueur (explosion, shake)
- Changement de vent (rotation de flèche)
- Highlight de base (pulse, glow)

### **4. Son**
Points d'ajout de SFX :
- Placement de lave : `PlaceLavaPiece()`
- Placement de bloque : `PlaceBlockPiece()`
- Eau qui enlève lave : `PlayWaterCard()`
- Changement de vent : `SetWindDirection()`
- Élimination : `CheckBaseElimination()`

---

## 📱 Adaptation Phygital

**Le jeu est conçu pour devenir phygital (détection de cartes physiques).**

**Points de remplacement futurs :**
1. `PlayerHand.cardsInHand` → Détection par caméra/RFID
2. `GameTester` input → Détection de placement de carte physique
3. Preview visuel → Projection sur table physique

**Architecture actuelle compatible :** Le système de cartes est découplé, donc facile à remplacer par une détection hardware.

---

## 🐛 Points d'Attention

### **Bugs Connus / Limitations :**
- ❌ Pas de système de sauvegarde
- ❌ Pas d'annulation de coup (undo)
- ❌ Le reset (touche R) détruit tout sans confirmation
- ❌ Pas de limite de temps par tour

### **Colliders Désactivés**
Les pièces (lave, bloques) ont leurs colliders **désactivés** pour que le raycast passe à travers et touche les tiles. Ne réactive pas ces colliders ou le jeu cassera.

### **Ordre d'Exécution**
`GridManager` doit s'exécuter **avant** `GameManager`. Vérifie dans **Project Settings → Script Execution Order**.

---

## 📞 Questions Fréquentes

**Q: Comment ajouter un nouveau type de carte ?**

R: Ajoute un `CardType` dans `CardSystem.cs`, puis crée une méthode `PlayXXXCard()` dans `GameManager.cs`

**Q: Comment changer le nombre de joueurs ?**

R: Change `numberOfPlayers` dans l'Inspector du GameManager (2-4 max)

**Q: Comment changer la taille de la grille ?**

R: Change `gridWidth` et `gridHeight` dans GridManager (doit être pair pour que le centre fonctionne)

**Q: Les previews sont trop petits/grands**

R: Ajuste `Vector3.up * 0.5f` dans `CardPlacementSystem.UpdatePreview()`

---

**Bon courage pour le front-end ! 🎨🚀**