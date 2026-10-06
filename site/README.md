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
La première colonne reste visible pendant le défilement sur petit écran. Des repères
accompagnent les statuts textuels ; les sources et la méthode se déplient sous le tableau.

À l’assemblage, les URLs des feuilles de styles et du script reçoivent une empreinte
de leur contenu pour éviter un mélange de nouvelle page et d’anciens styles en cache.

À chaque mise à jour, vérifier les sources officielles liées dans le tableau,
actualiser la date de consultation et conserver les mêmes faits dans `index.html`,
les traductions de `app.js` et le tableau anglais du README principal. « Non confirmé »
n’est pas une absence ; les fonctions observées sur le site de NotMeter sont
explicitement distinguées de celles du logiciel. Aucun test comparatif de précision
ou de capture concurrente n’a été réalisé.

## Aperçus

Les 6 images de `images/` sont des rendus démo de `tools/Verify.ps1`, avec l'interface
0.5.15 en anglais, dans les trois thèmes. `PublicPreviewFixture.cs` crée cinq personnages
fictifs et des chiffres inventés ; les noms et icônes des compétences viennent du catalogue
embarqué. Aucun combat réel n'est utilisé. Dans `artifacts/verification` :
- `overlay-en-{thème}.png` vient de `public-overlay-en-{thème}.png`.
- `report-en-{thème}.png` vient de `public-report-en-{thème}.png` (1424 × 900).

Les captures restent en anglais quelle que soit la langue du texte du site.
Le README principal utilise `public-overlay-en-dark.png`, `public-skills-en-dark.png`,
`public-report-en-dark.png` et `en-dark-history.png` du même dossier de vérification.
Lors du remplacement des aperçus, actualiser aussi leur paramètre de version dans
`index.html` et `app.js` pour rafraîchir les images mises en cache.

Seuls ces fichiers synthétiques explicitement nommés sont copiés. Ne jamais copier
un répertoire de diagnostics, un combat réel ou `docs/images/rapport-atiel.png` sur le site.
Les aperçus sont signalés comme fictifs et ne comparent pas les performances des classes.

Le nom technique `Spike.exe` est conservé pour les
mises à jour des installations existantes. Le bouton de téléchargement cible
`releases/latest/download/Spike.exe`, sans numéro de version figé ni appel API.

Les ressources AION 2 visibles dans les aperçus appartiennent à NCSOFT. Les notices
MIT, tierces et OFL sont publiées avec le site et accessibles depuis son pied de page.

## Activités et aperçus Windows

La section `#activites` présente les rappels, les objectifs et les réserves en FR/EN/ES,
avec contenu français sans JavaScript. Les six images `events-en-{thème}.png` et
`checklist-en-{thème}.png` sont des **rendus WPF Windows**, avec données de démonstration,
assemblés pour la présentation. Aucune interface n’est redessinée dans l’illustration.

Source reproductible : `tools/Preview-Activities.html`. Après `tools/Verify.ps1`, copier
uniquement les images `schedule-en-*`, `reserves-en-*`, `reserves-overlay-en-*` et
`notifications-en-*` et `upcoming-overlay-en-*` depuis `artifacts/verification` dans un répertoire public temporaire.
Y ajouter le HTML sous `index.html` et Geist Regular/SemiBold depuis les polices de
l’application. Servir **ce répertoire dédié**, jamais le dépôt entier. Capturer avec
Chromium à **1600 × 1000**, échelle 1, une fois les images et polices chargées, pour
`?theme=dark&view=events` et `?theme=dark&view=checklist`, puis les thèmes `light` et
`contrast`. Les sources des captures sont les vérifications Windows du commit publié.

Le script d’assemblage publie explicitement ces six fichiers. `app.js` adapte leurs
URLs au thème et leurs textes alternatifs à la langue. Recherche Global et limites :
`docs/ACTIVITIES.md`. Publier cette présentation avec la release Windows correspondante ;
le site seul ne met pas à jour les exécutables déjà téléchargés.

Les nouveaux aperçus Activités montrent les contrôles ±1 / ±40, les onglets dans
l’en-tête et les compteurs d’événements sur une seule ligne. `upcoming-overlay-en-{thème}.png`
complète la composition des rappels ; ces captures représentent des données fictives.
