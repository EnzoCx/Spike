# 0.4.10 — copie compacte pour le chat du jeu

- Le bouton Copier du rapport et de l’overlay produit une seule ligne : cible,
  durée, DPS/HPS observé, puis classement avec noms et pourcentages.
- Les libellés et les décimales suivent la langue choisie dans l’interface
  (français, anglais ou espagnol). Les valeurs sont abrégées en k/M.
- Les filtres, les archives sélectionnées, les sources non identifiées et les
  mentions de démonstration, d’import non vérifié et de soins bruts sont conservés.

Validation : contrôles hors ligne via `tools/Verify.ps1`, copie simulée en FR/EN/ES,
vérification du format sur une ligne et des nombres abrégés, interface dans les trois
thèmes. Aucun presse-papiers système modifié ni capture en jeu pendant les contrôles.

La longueur dépend du nombre de participants et de leurs noms ; aucune ligne du
classement n’est supprimée pour imposer une limite de caractères. Le collage dans
le chat du jeu n’a pas été testé en direct.
