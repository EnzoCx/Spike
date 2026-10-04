# 0.4.13 — un overlay plus discret

- Nouvelle présentation discrète par défaut : une ligne par joueur, DPS/HPS abrégés,
  fins repères de classe et commandes sans gros fonds colorés.
- La hauteur suit le classement, sans grand espace vide. Le défilement reste
  disponible au-delà de huit lignes et la largeur peut descendre à 320 px.
- Fond Graphite plus transparent, renforcé au survol pour lire les informations.
  Les textes restent opaques en combat ; les thèmes Ivoire et Contraste élevé
  gardent des traitements adaptés à leur lisibilité.
- Statistiques détaillées au survol, compétences au clic et rapport complet
  toujours accessibles. La présentation détaillée reste disponible dans les options.
- Copie encore raccourcie : cible, durée, DPS/HPS observé et noms avec leurs valeurs.
  Suppression des rangs, pourcentages et préfixe de marque dans le texte copié.
  Tous les participants, sources non identifiées et mentions de provenance restent présents.

Les préférences existantes et les archives sont conservées. La réduction après deux
minutes hors combat et la réouverture au prochain combat restent actives.

Validation hors ligne : `tools/Verify.ps1`, FR/EN/ES et trois thèmes, présentation
discrète/détaillée, taille minimale, migration des préférences, survol, copie simulée,
archives, maintien des lignes et réduction hors combat. Aucun collage ni capture
dans le jeu pendant les vérifications ; l’application ouverte n’est pas redémarrée.
