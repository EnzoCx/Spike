# 0.4.6 — mise à jour automatique

- Recherche d'une version stable plus récente sur les releases du dépôt officiel au démarrage.
- Téléchargement en arrière-plan et vérification de la taille, du SHA-256 fourni par GitHub
  et de la version Windows de l'EXE avant installation.
- Au lancement suivant, remplacement atomique de l'EXE puis démarrage de la nouvelle version.
  Aucun redémarrage imposé pendant une session ; préférences et combats conservés.
- En cas d'indisponibilité réseau ou d'échec d'installation, la version actuelle reste utilisable.
- Publication de l'EXE après validation hors ligne via un tag Git correspondant à la version.

La première installation de cette version se fait manuellement : les versions antérieures
ne disposent pas du mécanisme de mise à jour. Les EXE restent non signés.

Validation locale : `tools/Verify.ps1` réussi, compilation sans avertissement,
77 contrôles Core/mise à jour, régressions protocole et vérifications graphiques
FR/EN/ES dans les trois thèmes. Le passage entre deux exécutables de test vérifie
le redémarrage dans la nouvelle version, le repli si le fichier est verrouillé
et une nouvelle tentative après déverrouillage. Aucun test de capture en direct.
La publication du workflow et le téléchargement depuis une release publique
restent à valider lors de la première publication.
