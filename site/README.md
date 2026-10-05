# Spike — site public

Landing page statique FR/EN/ES, sans dépendances JavaScript, tracker ou police distante.
Identité de marque commune ; les aperçus montrent la nouvelle interface neutre du logiciel. Langue et thème mémorisés uniquement
dans le navigateur ; `?lang=fr`, `?lang=en` et `?lang=es` permettent un lien direct.
Sans JavaScript, le contenu français, les téléchargements et la FAQ restent disponibles.

`tools/Build-Site.ps1` assemble une liste explicite de fichiers publics dans
`artifacts/site`. Le workflow `pages.yml` ne publie que ce dossier sur GitHub Pages :
https://enzocx.github.io/Spike/. Aucun domaine personnalisé n’est nécessaire.

Pour prévisualiser : exécuter le script, servir `artifacts/site` avec un serveur
statique local, puis ouvrir son adresse. Ne jamais servir la racine du dépôt.

## Aperçus

Les 18 images de `images/` sont des rendus démo de `tools/Verify.ps1` version 0.5.3 :
- `overlay-{langue}-{thème}.png` vient du fichier de même nom dans `artifacts/verification`.
- `report-{langue}-{thème}.png` vient de `{langue}-{thème}-live.png`.

Seuls ces fichiers synthétiques explicitement nommés sont copiés. Ne jamais copier
un répertoire de diagnostics, un combat réel ou `docs/images/rapport-atiel.png` sur le site.
Les aperçus sont signalés comme fictifs et ne comparent pas les performances des classes.

Le nom technique `Spike.exe` est conservé pour les
mises à jour des installations existantes. Le bouton de téléchargement cible
`releases/latest/download/Spike.exe`, sans numéro de version figé ni appel API.

Les ressources AION 2 visibles dans les aperçus appartiennent à NCSOFT. Les notices
MIT, tierces et OFL sont publiées avec le site et accessibles depuis son pied de page.
