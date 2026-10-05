# Spike — Instrument de combat

Une identité sobre, précise et immédiatement reconnaissable pour un outil consulté pendant une partie. Le nom commercial est Spike depuis la version 0.5.0. Signature française : **Le combat, en clair.** Anglais : **Your combat, clearly.** Espagnol : **Tu combate, claro.**

## Le symbole

Un S segmenté en trois barres horizontales, tirées du classement du meter. Les coupes à 45° dessinent sa silhouette et évoquent l’impact. Le tracé reste identique dans l’application, l’overlay, l’icône Windows et les fichiers du site. Il ne reprend aucun emblème ou visuel d’AION.

Source unique : `symbol.path`, grille 64 × 64. `tools/Build-Brand.ps1` génère les SVG, PNG et ICO à partir de ce fichier ; le contrôle WPF lit directement ce même tracé.

- Symbole seul : minimum 16 px. Signature complète : minimum 110 px de large.
- Réserve autour du symbole : au moins un sixième de sa hauteur visible.
- Utiliser `logo-dark.svg` sur fond sombre, `logo-light.svg` sur fond clair, `logo-mono.svg` en monochrome.
- Ne pas étirer, faire pivoter, ajouter un contour, une ombre ou un effet lumineux. Garder les trois barres de la même couleur.
- Les SVG du logotype contiennent des lettres vectorisées : aucune police externe nécessaire pour les afficher.

## Couleurs

| Rôle | Graphite | Ivoire | Contraste élevé |
| --- | --- | --- | --- |
| Fond | `#191A18` | `#F3F0E8` | `#10120F` |
| Surface | `#252722` | `#E5E1D7` | `#1C201A` |
| Texte principal | `#F3F0E8` | `#242720` | `#FCFAF2` |
| Texte secondaire | `#B1B1A5` | `#5F6257` | `#E2E5D7` |
| Accent | `#DDA66A` | `#85501F` | `#FFD398` |
| Séparateur | `#3D4038` | `#C7C9BE` | `#929B87` |

Le bronze identifie la marque et la sélection active. Les couleurs des classes portent les données : ne pas remplacer toutes les barres par du bronze. Le texte d’un bouton accentué reprend la couleur du fond du thème. Sur fond clair, utiliser le bronze foncé, jamais le bronze clair pour du petit texte.

Le fond sombre est le choix par défaut pour limiter la gêne pendant le jeu. Les thèmes Ivoire et Contraste élevé conservent toutes les fonctionnalités. Le rang et les libellés rendent la couleur non indispensable à la lecture.

## Typographie et interface

**Barlow Condensed SemiBold** pour la signature et les titres de marque. **Barlow** pour les noms, actions et valeurs. Polices embarquées sous SIL OFL ; notices dans `src/Spike.Desktop/Fonts`.

- Échelle : 12, 15, 19, 24, 30 px ; l’overlay utilise 10–12 px pour les valeurs secondaires et 18 px pour sa signature.
- Les chiffres sont alignés à droite ; les noms sont alignés à gauche. Les unités restent visibles.
- Espacements : 4, 8, 12, 16, 24, 32 px. Rayons discrets, séparateurs fins, sans ombres décoratives.
- Une action principale accentuée par groupe. Les actions secondaires restent sur une surface neutre.
- Aucun clignotement ni animation décorative pendant le combat. Aucune décoration derrière les chiffres.
- Le logo reste dans les zones d’identité : en-tête, navigation, icône. Il ne remplace pas les icônes fonctionnelles.

## Application au site

Depuis 0.4.1, les repères de combat utilisent les emblèmes AION 2 et les couleurs
de classes du site NotMeter : voir `src/Spike.Desktop/GameArt/sources.json` et
`CREDITS.txt`. Ces couleurs de classes sont des conventions NotMeter, pas une
palette officielle NCSOFT. La marque Spike et les thèmes restent graphite /
bronze / ivoire. Les illustrations du jeu ne sont pas couvertes par notre licence MIT.

Reprendre la signature, la palette et la typographie. La liste de combats, les classements et les détails utilisent les mêmes codes que l’application. Un libellé visible distingue toujours un exemple d’un parse réel. `tokens.css` fournit les thèmes et les espacements ; les polices doivent être servies avec leurs notices OFL. Le site GitHub Pages reprend ces ressources ; voir `site/README.md`.

## Fichiers et génération

- `direction-graphique.png` : planche de direction avec aperçu réel de l’overlay.
- `logo-{dark,light,mono}.svg` : signatures complètes, lettres vectorisées.
- `logo-{dark,light,mono}.png` : mêmes signatures sur fond transparent, hauteur 256 px.
- `symbol-{dark,light,mono}.svg` : symboles seuls.
- `symbol-transparent.png` : symbole bronze transparent, 1024 px.
- `spike.ico` : icône Windows, 16 / 24 / 32 / 48 / 64 / 128 / 256 px.
- `icon-*.png` : icônes séparées, utilisables notamment pour le web.
- `social-cover.png` : couverture pour une future présentation du projet.
- `tokens.css` : palette, typographie et espacements pour le web.

Régénération sous Windows, sans accès réseau :

```powershell
powershell -NoProfile -STA -File tools/Build-Brand.ps1
```

Les éléments dessinés pour ce projet suivent sa licence MIT. Spike est le nom du produit ; aucune recherche juridique d’antériorité de marque n’a été effectuée. Depuis 0.5.2, les projets, l’exécutable Spike.exe et les nouveaux chemins de sauvegarde portent ce nom. Les anciennes données DPSMeter sont copiées au premier lancement, sans supprimer les originaux.
