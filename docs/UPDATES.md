# Mises à jour automatiques

L'EXE autonome consulte `https://api.github.com/repos/EnzoCx/Spike/releases/latest`
une fois à chaque démarrage normal, sans authentification ni données de combat.
Après le changement de compte et de dépôt, les versions 0.5.0 et antérieures doivent
télécharger manuellement la version 0.5.1 une fois : leur validation stricte refuse
les assets dont l'adresse utilise le nouveau dépôt. Les versions suivantes utilisent
la nouvelle adresse sans assouplir la vérification du dépôt officiel.
Les diagnostics et builds de développement ne consultent pas GitHub.
Le démarrage et la capture n'attendent pas le réseau. Aucun dialogue ni redémarrage forcé.

Seules les releases stables avec une version numérique supérieure sont acceptées.
L'asset doit s'appeler `DPSMeter.exe`, provenir du dépôt officiel, mesurer au maximum
300 Mio et posséder un digest SHA-256 fourni par GitHub. Un téléchargement incomplet
ou incorrect ne devient jamais une mise à jour installable. Le téléchargement peut durer
au maximum cinq minutes ; sa préparation réseau est bornée à trente secondes.

Le cache `%LOCALAPPDATA%/DPSMeter/updates/` est séparé par chemin d'installation et
protégé contre les accès simultanés par un verrou de fichier. Au lancement suivant,
l'application revalide le cache et la version Windows du binaire. Une copie de l'EXE
actuel sert d'assistant sans fenêtre : elle attend la sortie du processus de démarrage,
remplace atomiquement l'EXE et relance celui-ci. Elle ne termine aucun processus.
Un lancement issu de cet assistant ne retente pas l'installation, pour éviter les boucles.
Une version égale ou supérieure déjà présente n'est jamais rétrogradée.

L'ancien EXE est conservé à côté, avec le suffixe `.previous`, pour récupération manuelle.
Les préférences et les combats ne sont jamais modifiés par l'assistant. Un dossier non
accessible en écriture ou un autre meter qui verrouille l'EXE peut empêcher l'installation :
l'ancienne version est relancée et une prochaine ouverture pourra retenter l'opération.
Il n'y a ni élévation administrateur, ni changement de pilote, ni installation de service.
Un réseau indisponible, une limite GitHub ou une release absente est ignoré sans bloquer
l'utilisation. Si l'application ferme avant la fin du téléchargement, il sera retenté.

Le SHA-256 vérifie l'intégrité par rapport aux métadonnées GitHub ; ce n'est pas une
signature de l'éditeur. La sécurité de publication dépend du compte et du dépôt GitHub.
L'application n'a pas encore de signature Authenticode ni de retour arrière automatique
en cas de régression fonctionnelle de la nouvelle version.

## Publier une version

1. Augmenter `Version` dans `src/DPSMeter.Desktop/DPSMeter.Desktop.csproj` et rédiger
   `docs/RELEASE-X.Y.Z.md`.
2. Exécuter `tools/Verify.ps1` et examiner les changements indexés.
   Ne pas publier si les vérifications échouent ou si le travail est incomplet.
3. Pousser le commit sur `main` puis son tag `vX.Y.Z`. Le workflow `release.yml` vérifie la correspondance
   du tag et de la version, exécute les contrôles hors ligne et construit l'EXE autonome.
4. Il prépare une release brouillon avec l'EXE puis la rend publique et la marque Latest.
   Un échec laisse au plus un brouillon à inspecter ; ne pas remplacer silencieusement
   les assets d'une version déjà publiée. Publier une nouvelle version corrective.
5. Vérifier la réussite du workflow et la disponibilité de `DPSMeter.exe` sur la release avant
   d'annoncer la publication terminée.

La première version équipée de ce système doit être téléchargée manuellement depuis
GitHub Releases. Les versions 0.4.5 et antérieures ne peuvent pas découvrir ce mécanisme.

## Vérifications hors ligne

`tools/Verify.ps1` inclut les tests de téléchargement simulé et `tools/Verify-Updates.ps1`.
Ce dernier publie deux versions d'un petit exécutable de test qui utilise le même code
de mise à jour : remplacement, sauvegarde, redémarrage effectif dans la nouvelle version,
chemins avec espaces, maintien de l'ancienne version si verrouillée, puis nouvelle tentative.
Aucune capture, fenêtre visible ou requête GitHub ; les caches de ces fixtures sont nettoyés.

Référence du contrat GitHub : https://docs.github.com/en/rest/releases/releases#get-the-latest-release
