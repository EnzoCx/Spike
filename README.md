# DPSMeter

Application Windows pour **AION 2 Global**, avec capture passive, analyse des combats et historique local. Projet indépendant de NCSOFT.

Identité graphique : **Instrument de combat**, graphite / bronze / ivoire. Logo vectoriel, icône Windows, déclinaisons et règles d’utilisation dans `brand/README.md`. **Version 0.4.5** : préparation du dépôt public, code organisé, dépendances verrouillées et validation automatisée. Les sources non identifiées sont séparées des joueurs, avec leurs dégâts conservés. Transparence hors combat depuis 0.4.3, aimantation et gestes rapides depuis 0.4.2 ; véritables emblèmes AION 2, icônes de compétences et couleurs de classes issues de NotMeter depuis 0.4.1. Voir `docs/RELEASE-0.4.4.md`.

## Reprendre le développement

Lire [AGENTS.md](AGENTS.md), puis [le guide de reprise](docs/HANDOFF.md).
Voir aussi [architecture](docs/ARCHITECTURE.md), [contribution](CONTRIBUTING.md),
[format JSON](docs/FORMAT.md) et [notices tierces](THIRD-PARTY-NOTICES.md).

```powershell
powershell -NoProfile -File tools/Verify.ps1
```

La CI Windows produit un exécutable autonome dans les artefacts d’une exécution réussie
[Build and verify](https://github.com/Phobie53/DPSMeter/actions/workflows/build.yml).
Ces builds ne sont pas signés. Le dépôt ne contient aucun combat réel ni paquet réseau.

## Utilisation

Ouvrir `DPSMeter.exe` à la racine du projet ou dans `artifacts/windows`.
La capture démarre automatiquement ; jouer normalement. Aucun redémarrage du jeu nécessaire.
L’overlay s’ouvre également au démarrage à partir de la version 0.3.2. Le bouton **Afficher / Masquer l’overlay**, en haut à droite de tous les écrans, et `Ctrl+Alt+M` permettent de le retrouver. L’ouverture automatique peut être désactivée dans les réglages.
Npcap doit déjà être installé : il n'est pas redistribué. L'application embarque .NET et ses tables dans un EXE.

- **En direct / Rapport** : classement et compétences côte à côte, dégâts/soins, DPS/HPS observés, filtre de cible, recherche de compétences, critiques, ticks, plus gros impact et impact moyen. Déplier **Rythme du combat** pour la courbe du groupe ou du joueur sélectionné.
- **Historique** : sauvegarde après 12 secondes sans événement, point de reprise toutes les 10 secondes, recherche ; sélectionner un combat puis **Rapport complet**, double-clic ou Entrée. Joueurs, compétences, critiques, ticks, cibles, soins et courbe restent consultables après fermeture. La capture continue pendant la consultation.
- **Mini-meter** : boss et durée, emblèmes et couleurs de classe, DPS/HPS, dégâts, contribution et critiques observés. Le survol affiche une fiche avec les huit principales compétences, coups, ticks et critiques. Cliquer sur un joueur affiche ses compétences ; la flèche revient au classement et **Rapport complet** ouvre l’analyse du joueur avec le même périmètre. Le menu **En direct ▾** permet de sélectionner les combats enregistrés, même après redémarrage. Les lignes sont actualisées sans être recréées à chaque seconde.
- **Présentation** : menu **···**, hauteur automatique (jusqu’à huit lignes), mode compact, opacité du fond sans rendre les chiffres transparents. Les cinq joueurs d’un groupe tiennent dans l’overlay par défaut. PV observés et puissance du personnage s’affichent lorsqu’ils ont été reçus ; les anciennes sauvegardes ne contiennent pas ces informations.
- **Discrétion hors combat** : l’ensemble de l’overlay passe à 15 % d’opacité après la fin du combat détectée (12 secondes sans événement), avec une transition de 300 ms. Il redevient immédiatement lisible à la reprise du combat ou au survol si déverrouillé. Déplacement, redimensionnement, menus et consultation d’un combat archivé restent lisibles. Option activée par défaut, mémorisée dans **··· → Presque transparent hors combat**. Le verrouillage conserve les clics traversants et le retour automatique en combat.
- **Retrouver un combat** : recherche par boss ou nom de joueur, filtre **Combats de boss**, durée, participants et DPS directement dans l’historique.
- **Placement de l’overlay** : glisser le titre, redimensionner par le coin inférieur droit ; taille et position sont mémorisées. Le bouton ◇ verrouille la fenêtre pour laisser les clics traverser. `Ctrl+Alt+M` la masque ; un second appui la rouvre déverrouillée. Overlay de bureau toujours au premier plan, prévu pour le mode fenêtré / sans bordure du jeu.
- **Aimantation** : à proximité d’un bord, la fenêtre s’aligne sur l’écran concerné, en tenant compte de sa barre des tâches. Maintenir **Maj** pour un déplacement libre ; option persistante dans **···**. Clic droit sur le titre pour les options, double-clic pour le mode compact. Le menu de position propose les quatre coins et le centre. **Réglages → Recentrer l’overlay** le ramène sur l’écran du logiciel et le déverrouille. Après changement d’affichage, une fenêtre hors écran est ramenée dans une zone accessible. La hauteur automatique conserve l’alignement inférieur lorsqu’il est utilisé.
- **Réglages** : français/anglais/espagnol, trois thèmes, nom facultatif du personnage, démarrage automatique et maintien au premier plan.
- **Pause** : les événements pendant la pause sont ignorés, jamais rejoués.
- **Nouveau combat** : archive le segment actuel ; le suivant commence au prochain événement.
- **Export** : JSON v2 avec remplacement des noms/identifiants des joueurs. Aucun envoi en ligne.
- **Import** : JSON v2 du projet, marqué non vérifié. Pas d'import NotMeter ou v1 du prototype précédent.

Au démarrage en milieu de partie, des joueurs/boss restent inconnus jusqu'à ce que le jeu renvoie leur identité. Les noms de compétences disposent de tables FR/EN/ES avec repli anglais. Le nom facultatif s'applique à la prochaine capture.

Les sources anonymes ne lançant que Mur de feu, Tempête glaciale ou Piège explosif sont provisoirement classées **Sources à identifier**, sans affirmer qu’elles sont des joueurs ou leur attribuer un propriétaire. Les entités apparues comme PNJ utilisant des compétences de classe restent également séparées tant que leur propriétaire est inconnu. Dans l’overlay, une ligne dépliable conserve leurs détails et leur contribution ; leurs dégâts restent inclus dans le total, leur nombre est exclu du nombre de joueurs. Un véritable joueur sans nom utilisant d’autres compétences reste affiché. Les anciens rapports locaux bénéficient de cette distinction à la lecture, sans modification de fichier. Cela ne constitue pas un filtre exhaustif du groupe ni une résolution complète des invocations.

## Mesures et limites

DPS = dégâts / durée, minimum une seconde. Par défaut, les dégâts portent sur le boss principal : durée entre le premier et le dernier dégât des joueurs sur cette cible. Les soins et les dégâts ailleurs ne prolongent pas ce temps. Le mode **Tout / Toutes les cibles** conserve la durée entre le premier et le dernier événement du segment. L’attente de fin de combat ne fait plus baisser le DPS. Les périodes entre attaques restent incluses. Les ticks ajoutent des dégâts, sans gonfler le nombre de coups ni les critiques. Les soins sont bruts, sursoins non retranchés.

Les variantes de niveau/spécialisation d’une compétence de classe sont regroupées par ID de base et nom ; les événements originaux sont conservés. Les PV maximum affichés sont le maximum **observé**, qui peut être inférieur au maximum réel. Les buffs, attaques de dos/de face, doubles et coups parfaits ne sont pas encore exposés par ce moteur : aucune statistique de remplacement n’est inventée.

Les joueurs affichés sont ceux identifiés dans le flux observé, pas nécessairement uniquement le groupe. Les critiques reçus pour les autres joueurs peuvent être incomplets. Une longue phase sans événements peut séparer un combat : la segmentation n'est pas une reconnaissance exhaustive des rencontres/phases. Les invocations reconnues sont attribuées à leur propriétaire par le moteur amont. Une mise à jour du jeu peut nécessiter d'adapter le décodeur.

Le site communautaire, les classements, la signature Windows et la redistribution Npcap restent à développer. Aucun service n'est provisionné ou facturé.

## Stockage

- `%LOCALAPPDATA%/DPSMeter/settings.json` : préférences.
- `%LOCALAPPDATA%/DPSMeter/fights/` : combats locaux, noms inclus.
- `%LOCALAPPDATA%/DPSMeter/engine/` : tables embarquées, extraites au premier lancement.

Tous les combats valides du dossier sont listés, sans limite de 500 ni suppression automatique. Le chargement s’effectue en arrière-plan ; une très grande collection peut prendre plus de temps. Les fichiers invalides sont ignorés. Aucun paquet brut, IP, compte du jeu ou télémétrie n'est conservé par l'application.

## Validation du 4 octobre 2026

- 53 contrôles de calcul, import/export, séparation soins/dégâts, cibles et historique : fenêtre du boss, regroupement des variantes, métadonnées facultatives, anciens fichiers, réouverture du stockage et accès à plus de 500 combats.
- Régressions protocole : exemples de trames dégâts/critiques/soins, fragmentation TCP, retransmissions, pause et décompression LZ4 bornée.
- 27 rendus du tableau de bord et 12 rendus d’overlay hors écran en FR/EN/ES et dans les trois thèmes. Interactions vérifiées : compétences au clic, contenu du survol, retour, recherche, filtres, passage dégâts/soins, hauteur automatique/compacte et rapport archivé pendant les mises à jour du direct.
- Aperçu de l’overlay généré avec un combat réel déjà enregistré. Placement par-dessus le jeu et clics traversants non testés en situation, pour ne pas interrompre la partie.
- Test passif Global : 3 541 événements en 120 secondes, aucune erreur de capture ni événement abandonné.
- Chaîne application : 3 075 événements, sauvegarde et relecture des mêmes totaux, six joueurs affichables dont cinq nommés. Aucune fenêtre affichée au-dessus du jeu.
- Comparaison avec la capture NotMeter fournie sur Atiel : les cinq DPS arrondis et les 52,08 k DPS du groupe correspondent ; Coup au cœur totalise 630 666 dégâts et 123 coups après regroupement. Ce résultat ne constitue pas une validation exhaustive de toutes les classes, rencontres, flags critiques, durées ou configurations VPN. Le survol physique pendant le jeu n’a pas été testé ; ses composants et son branchement ont été vérifiés hors écran.

## Développement

SDK .NET 9 sous Windows. Prévoir .NET 10 LTS avant une diffusion durable.

```powershell
dotnet build DPSMeter.sln -c Release -m:1
dotnet run --project tests/DPSMeter.Checks -c Release
dotnet run --project tests/DPSMeter.EngineChecks -c Release
dotnet publish src/DPSMeter.Desktop -c Release -o artifacts/windows -m:1
```

Les tests ordinaires sont hors ligne. La capture de diagnostic nécessite `--probe` (EngineChecks) ou `--verify-live` (application). Ne pas installer de pilote ni modifier le réseau pendant une partie.
`--verify-views <dossier>` rend les écrans hors fenêtre ; `--render-file <combat.json> <dossier>` rend un combat sauvegardé sans capture ni fenêtre.

## Licence et règles

Visuels AION 2 © NCSOFT, récupérés sur les ressources publiques de NotMeter.
Le catalogue Global embarqué associe 15 671 identifiants de compétences et 3 693
identifiants de buffs aux tuiles d’un atlas ; ce ne sont pas autant d’images distinctes.
Neuf emblèmes de classes sont inclus. Les icônes s’affichent sans accès réseau,
par identifiant et non par traduction du nom. Un identifiant inconnu garde un repère
neutre. Les images de buffs sont prêtes, mais le suivi d’uptime reste à développer.
Sources et empreintes : `src/DPSMeter.Desktop/GameArt/sources.json`. Notices dans
les réglages et `GameArt/CREDITS.txt`. Réimport pour développeurs :
`python tools/Fetch-GameArtwork.py` (Pillow nécessaire uniquement à la construction).
Les empreintes bloquent les changements distants silencieux. Les illustrations
du jeu ne relèvent pas de la licence MIT de notre code.

MIT pour notre code et le moteur dérivé de SkeeveTV : voir `src/DPSMeter.Engine/Vendor/ORIGIN.md` et `Vendor/LICENSE`.
Polices Barlow/Barlow Condensed sous SIL OFL 1.1 ; notices consultables dans les réglages. Les noms/données du jeu restent la propriété de NCSOFT.
La capture passive n’est pas une garantie de conformité aux CGU. Aucune approbation de NCSOFT revendiquée. Aucune injection, lecture mémoire du jeu, modification de paquet, automatisation ou contournement de protection.
