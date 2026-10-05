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

## Comparatif

La section `#comparatif` compare Spike, NotMeter, A2Tools et Abyss Logs. Le tableau
français reste lisible sans JavaScript ; `app.js` fournit les versions EN/ES. Le
conteneur défile au clavier et sur mobile, avec les couleurs des trois thèmes.

À chaque mise à jour, vérifier les sources officielles liées dans le tableau,
actualiser la date de consultation et conserver les mêmes faits dans `index.html`,
les traductions de `app.js` et le tableau anglais du README principal. « Non confirmé »
n’est pas une absence ; les fonctions observées sur le site de NotMeter sont
explicitement distinguées de celles du logiciel. Aucun test comparatif de précision
ou de capture concurrente n’a été réalisé.

## Aperçus

Les 6 images de `images/` sont des rendus démo de `tools/Verify.ps1`, avec l'interface
0.5.3 en anglais (y compris les noms des compétences fictives), dans les trois thèmes :
- `overlay-en-{thème}.png` vient du fichier de même nom dans `artifacts/verification`.
- `report-en-{thème}.png` vient de `en-{thème}-live.png`.

Les captures restent en anglais quelle que soit la langue du texte du site.
Le README principal utilise `overlay-discreet-en-dark.png`, `overlay-skills-en-dark.png`,
`en-dark-live.png` et `en-dark-history.png` du même dossier de vérification.

Seuls ces fichiers synthétiques explicitement nommés sont copiés. Ne jamais copier
un répertoire de diagnostics, un combat réel ou `docs/images/rapport-atiel.png` sur le site.
Les aperçus sont signalés comme fictifs et ne comparent pas les performances des classes.

Le nom technique `Spike.exe` est conservé pour les
mises à jour des installations existantes. Le bouton de téléchargement cible
`releases/latest/download/Spike.exe`, sans numéro de version figé ni appel API.

Les ressources AION 2 visibles dans les aperçus appartiennent à NCSOFT. Les notices
MIT, tierces et OFL sont publiées avec le site et accessibles depuis son pied de page.
