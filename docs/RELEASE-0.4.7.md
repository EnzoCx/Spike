# 0.4.7 — assistant de première installation

- Si Npcap manque, un assistant guide le premier lancement : site officiel,
  étapes d’installation et vérification sans quitter DPSMeter.
- « Continuer » devient disponible après détection ; « Plus tard » permet
  de consulter les archives et réglages. « Démarrer » rouvre l’assistant si nécessaire.
- Si Npcap est déjà installé, le démarrage habituel est conservé.
- Assistant disponible en français, anglais et espagnol, dans les trois thèmes.

Npcap reste installé séparément par l’utilisateur. Sa détection confirme la présence
des fichiers requis, pas la réception effective des combats. Aucun pilote embarqué
ni installation automatique.

Validation : `tools/Verify.ps1` réussi, compilation sans avertissement ; contrôles
Core, protocole, mises à jour et interface hors écran. Les scénarios Npcap absent,
nouvellement installé, retiré avant confirmation et installation reportée utilisent
une détection simulée. Rendus dans les trois langues et thèmes à la taille minimale.
Aucune installation réelle de pilote ni capture en jeu pendant ces contrôles.
