# 0.4.11 — overlay réduit après le combat

- Après deux minutes hors combat, le mini-meter se réduit à une barre de titre de
  44 pixels de haut. L’espace libéré ne bloque plus les clics dans le jeu.
- Le passage de la souris ne déplie plus le classement réduit. Le bouton flèche
  permet de le rouvrir pour deux minutes ; le prochain combat le rétablit automatiquement.
- La taille choisie, le verrouillage des clics et l’ancrage en bas de l’écran sont
  conservés. La lecture des archives reste dépliée ; un menu ouvert ou un déplacement
  en cours retarde la réduction. La transparence hors combat reste indépendante.
- Le bouton est traduit en français, anglais et espagnol dans les trois thèmes.

Validation : `tools/Verify.ps1`, dont 342 contrôles de réduction/réouverture hors écran
avec fenêtres natives invisibles, dimensions manuelles/automatiques, survol, archives,
reprise du combat et conservation du placement. Contrôle du formatage des fichiers modifiés.
Aucune capture ni interaction avec le jeu pendant ces vérifications ; comportement en
session réelle non testé.
