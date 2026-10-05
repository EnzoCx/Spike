# Faisabilité du rDPS dans Spike

Étude du 5 octobre 2026, prolongée par une **bêta partielle** à la demande du joueur.
Les sources publiques donnent une piste pour lire les buffs, mais ne démontrent
pas encore un calcul fiable sur AION 2 Global. Le modèle livré, ses hypothèses et
son utilisation sont décrits dans [RDPS.md](RDPS.md). Les outils Python restent
des expériences hors ligne, distinctes de l’estimation intégrée à l’application.

## Résultat de l’étude

La limite initiale venait en partie du décodeur : il ne traitait pas les messages
candidats de buffs. La bêta les observe désormais. Les octets `2A 38` et `2B 38` figuraient déjà dans
`src/Spike.Engine/assets/aion2/protocol/opcodes.json`, parmi les opcodes de
synchronisation observés. Cela prouve leur présence dans cette liste, pas la
signification de leur contenu ni une couverture exhaustive des buffs.

Le suivi des buffs ne suffit pas : il faut aussi les coefficients par niveau et
spécialisation, les règles de cumul et parfois les statistiques propres à chaque
attaquant. Aucun coefficient n’a été ajouté au catalogue `rdps_watchlist.json`.
Son marqueur `calibrated: false` reste justifié.

## Sources consultées

Les liens de code sont figés sur les révisions inspectées. Une annonce de fonction
par un autre outil n’est pas une validation de précision.

| Source | Observation et portée |
| --- | --- |
| [RATmeter, processeur de buffs](https://github.com/Kuroukihime/AIon2-Dps-Meter/blob/2e8d67d62cef0532f10aecb2df5426b50f4d2f79/AionDpsMeter.Services/PacketProcessing/Processors/BuffPacketProcessor.cs) et [opcodes](https://github.com/Kuroukihime/AIon2-Dps-Meter/blob/2e8d67d62cef0532f10aecb2df5426b50f4d2f79/AionDpsMeter.Services/PacketProcessing/Routing/PacketOpcodes.cs) | Décrit un format contenant destinataire, effet, durée et auteur pour deux messages. Ne démontre pas les retraits anticipés ni la validité Global. Dépôt GPL-3.0 ; aucun fichier source incorporé à Spike. |
| [JaMeter, méthode nDPS](https://www.jameter.net/guide/ndps) | Décrit le retrait des bonus externes par coup. Signale des approximations lorsque les statistiques de base ou les niveaux manquent, et pour certains effets non linéaires. |
| [Aion2t, explication du développeur](https://dev.to/aionon/reverse-engineering-an-mmo-aion-2s-network-protocol-to-build-a-real-time-dps-meter-rust-tauri-3157) | Décrit la réattribution des dégâts et les difficultés de cumul, d’identité et de niveau. Ne fournit pas un protocole Global validé ni un moteur réutilisable. |
| [SkeeveAN, amont MIT](https://github.com/SkeeveAN/Aion-DPS-Meter/tree/324fefe910e637408997f8be1f2a7f3c7afc19d7) | L’arbre inspecté contient une liste rDPS et des noms de buffs ; pas de module rDPS ou de suivi des buffs identifié prêt à intégrer. |
| [A2Tools](https://github.com/taengu/A2Tools-DPS-Meter/tree/db1079fffe85bc7fb5ef1f7b85859e181b6cd941) | Source GPL-3.0 disponible, mais pas de module de buffs/rDPS identifié dans l’arbre inspecté. Aucun code repris. |
| [Aion2t, distribution](https://github.com/Grachy/aion2t-dps-meter) et [Abyss Logs](https://github.com/karim-mo/aion2-abysslogs-dps-meter) | Les arbres inspectés sont des distributions/documentations ; ils ne fournissent pas un moteur rDPS à intégrer. |

Les faits de format ci-dessous servent d’hypothèses d’interopérabilité. L’analyseur
Python a été écrit pour cette étude ; aucun code GPL ni binaire décompilé n’est
incorporé au moteur ou à l’application. Les données de jeu restent attribuées à
NCSOFT. La licence de Spike n’est pas une licence sur les catalogues tiers.

## Compétences candidates pour la première validation

Les pages suivantes déclarent des données client du 18 septembre 2026. Ce sont des
valeurs affichées par un catalogue tiers, à vérifier sur la version Global jouée,
pas une calibration. Leur pourcentage ne prouve pas un multiplicateur final.
La bêta emploie néanmoins cette hypothèse simplifiée, explicitement signalée
et versionnée ; elle devra être corrigée ou abandonnée selon les essais en jeu.

| Compétence | Informations utiles observées | Point à vérifier |
| --- | --- | --- |
| [Light of Protection, 17410000](https://aion2.app/db/skills/17410000) | Niveau 1 : bonus PvE annoncé de 10,5 %, effet activable pour soi et les membres proches. | Auteur et bénéficiaires réels, sortie de portée, niveau et désactivation. |
| [Undefeated Mantra, 18190000](https://aion2.app/db/skills/18190000) | Même bonus annoncé au niveau 1. Avec Light of Protection, le niveau supérieur prévaut ; à égalité, priorité à la mantra. Des spécialisations affectent aussi critique et double coup. | Priorité effective, bonus secondaires, deux auteurs de même classe. |
| [Chain of Torment, 17070000](https://aion2.app/db/skills/17070000) | Au niveau 1 : baisse de tolérance PvE annoncée de 5 %, dégâts directs et périodiques. Une spécialisation modifie la réduction. | Séparer les dégâts du sort de l’amplification pour autrui ; durée du débuff distincte de celle du DoT. |

## Hypothèse de lecture des messages

Entrée attendue : trame reconstituée, préfixe de longueur retiré, bundles déjà
décompressés. Opcode lu en ordre réseau, entiers fixes du corps en little endian.
Les champs n’ont pas tous une signification confirmée.

| Ordre | Type | Interprétation candidate |
| --- | --- | --- |
| 1 | 2 octets | `2A 38` ou `2B 38` |
| 2 | varuint32 | Bénéficiaire |
| 3 | octet, octet | Drapeau et type non interprétés |
| 4 | varuint32 | Champ inconnu, éventuellement instance |
| 5 | uint32 | Identifiant d’effet ; division entière par 10 utilisée par le catalogue tiers |
| 6 | uint32 | Durée en millisecondes |
| 7 | uint32, uint32 | Champ inconnu puis horloge candidate |
| 8 | varuint32 | Auteur |
| 9 | reste éventuel | Octets non interprétés, comptés explicitement |

Une durée nulle n’est pas classée comme retrait. Un auteur nul reste inconnu.
Une durée annoncée ne prouve pas la durée effective. Une correspondance de structure
ne prouve ni un buff actif, ni son niveau, ni un groupe confirmé.

## Outils hors ligne

`tools/rdps/inspect_frames.py` lit uniquement un fichier local fourni explicitement.
Il ne peut ni lancer Npcap, ni ouvrir le jeu, ni envoyer des données. Il n’est pas
branché dans l’application. Python 3.10 ou supérieur, bibliothèque standard seule.

```powershell
python -m unittest discover -s tools/rdps -p 'test_*.py' -v
python tools/rdps/inspect_frames.py artifacts/rdps-frames.jsonl
```

Format d’entrée, un objet par ligne : `{"atMs":0,"frameHex":"043800"}`.
Cet exemple fictif illustre seulement l’enveloppe, pas un buff. Le lecteur accepte
au plus 64 Mio, 250 000 trames, 16 384 octets par trame et 24 heures, dans l’ordre
chronologique. Il limite les exemples du rapport à 1 000 mais compte toutes les
trames. Les bundles non décompressés et les candidats tronqués sont signalés.

Le rapport remplace les IDs d’entités par des alias locaux, omet les octets bruts
et les champs inconnus de type horloge/instance, et conserve les IDs d’effets pour
l’étude. Il reste explicitement `research-only-unvalidated`, avec
`usableForRdps: false`, même si toutes les trames respectent la structure. Les alias
ne résolvent pas les propriétaires ni les IDs réutilisés. Examiner un rapport avant
de le partager ; aucun partage automatique.

Ce format n’est pas un import de combat v2 ni un lecteur PCAP. Aucun enregistreur
n’est activé ou livré par cette étude. Pour un futur essai, préparer une collecte
locale bornée et volontaire des seules données nécessaires, puis convertir ses
trames dans ce format. Ne pas activer le dump général désactivé dans le moteur.

## Calculs vérifiés et limites

`test_math_hypotheses.py` vérifie trois expériences exactes avec des fractions.
Il ne contient aucune formule présentée comme celle d’AION 2.

- Multiplicateur final externe connu de 1,2 : un coup de 12 000 comprend 10 000
  personnels et 2 000 apportés. Retirer 20 % du résultat serait incorrect.
- Hypothèse d’un bonus ajouté à une même statistique :
  `sansBuff = dégâts × (1 + base) / (1 + base + bonus)`.
  Pour un coup de 12 000 et un bonus de 20 %, une base de 20 % donne environ
  10 285,71 sans buff, une base de 80 % donne 10 800. Les mêmes dégâts et le même
  buff ne déterminent donc pas seuls la contribution si cette règle s’applique.
- Deux multiplicateurs hypothétiques de 1,1 et 1,2 font passer 10 000 à 13 200.
  Retirer chacun indépendamment attribuerait 3 400 de bonus au lieu de 3 200.
  Une convention de partage de l’interaction est nécessaire. La moyenne des deux
  ordres donnerait 1 100 et 2 100 ; ce n’est ni une règle du jeu validée ni une
  convention choisie pour Spike à ce stade.

Le rDPS bêta conserve le total des dégâts entre les participants, y compris
les sources inconnues, et la fenêtre temporelle du boss existante. Les bonus
personnels restent personnels. Un manque d’observation ne devient jamais un bonus
nul. Les archives v2 existantes restent lisibles, avec rDPS indisponible.

## Prochain essai nécessitant le joueur

Première étape : connaître sa classe et la disponibilité d’un clerc ou d’un aède.
Après accord pour une collecte locale ciblée, procéder sur un mannequin si possible,
sinon une cible reproductible, sans changer équipement, compétence ou spécialisation.

1. Noter région, version, buff, niveau, spécialisations et statistiques affichées.
2. Observer l’activation, le renouvellement, la désactivation et la sortie de portée,
   même sans infliger de dégâts. Vérifier l’auteur et chaque bénéficiaire.
3. Alterner plusieurs séries sans buff et avec un seul buff. Séparer les critiques,
   doubles et autres effets ; un unique coup de chaque côté ne calibre rien.
4. Refaire une série indépendante. Si un changement de statistique personnelle
   modifie le ratio, le coefficient nominal seul ne suffit pas.
5. Ensuite seulement : concurrence entre buffs, débuffs, invocations, pertes de
   trames, début de capture tardif et renouvellement des IDs.

Le critère de passage est une identité non ambiguë, un cycle de vie vérifié et des
prédictions reproduites sur des observations distinctes de celles de calibration.
Les écarts doivent être expliqués par les arrondis et la variation mesurée ; aucune
tolérance arbitraire ne transforme une erreur de modèle en validation.

## État de livraison

Vérifications exécutées le 5 octobre : les 14 tests Python sont passés, ainsi que
`powershell -NoProfile -File tools/Verify.ps1` (code de sortie 0, build, contrôles
Core/Engine, mise à jour simulée, publication locale et vues invisibles FR/EN/ES,
trois thèmes). L’aide de l’analyseur a aussi été exécutée. Le journal de vérification
reste dans `artifacts/rdps-offline-verification.log`, ignoré par Git. Ces contrôles
ne valident pas les hypothèses de protocole sur de vrais buffs.

Suite à cette étude, le joueur a demandé la publication d’une bêta avant de pouvoir
tester en jeu. L’application inclut donc le suivi expérimental de deux auras, un
calcul conservatif des totaux, l’interface FR/EN/ES et des métadonnées v2 facultatives.
Le site et le README annoncent explicitement les limites. La validation de précision
reste à faire ; aucune session en jeu ni nouvelle capture n’a été lancée pour ce travail.
