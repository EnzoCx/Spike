# Décisions actuelles

## Client local d’abord

Windows WPF, AION 2 Global, FR/EN/ES et trois thèmes. Une application utilisable sans
compte, un overlay séparé du jeu et un historique conservé localement.
Un EXE autonome embarque .NET, les polices et les catalogues ; Npcap reste un prérequis
externe, jamais installé automatiquement. Aucune infrastructure payante n’est provisionnée.

## Acquisition

Capture passive avec Npcap existant et décodeur dérivé de SkeeveAN/Aion-DPS-Meter,
commit `2f237fed139fe4a173c9de9d43cdeb0eb52453f8`. Pas d’injection, lecture mémoire,
modification de trafic ni automatisation du jeu. Aucune approbation de NCSOFT revendiquée.
Les expériences live sont distinctes des tests ordinaires et ne doivent pas interrompre
une partie en cours. La disponibilité technique n’est pas une garantie de conformité.

## Fiabilité des chiffres

Calculs partagés entre rapport et overlay. Les événements originaux restent conservés.
Une source non identifiée reste distincte d’un joueur et ne reçoit pas de propriétaire
supposé. Les critiques des autres joueurs peuvent être incomplets. Voir HANDOFF.md.

## Dépôt public

Code original sous MIT ; conserver les notices amont et distinguer les images/données
du jeu et les autres licences. Aucun historique personnel, paquet brut ou screenshot de
partie réelle dans le dépôt. Les visuels de démonstration utilisent des données fictives.
Les dépendances sont verrouillées et la CI Windows reprend les mêmes contrôles que le local.

## Futur site communautaire — non implémenté

Préférer un monolithe simple (API ASP.NET Core et PostgreSQL) avec partage volontaire :
apercu des données, validation serveur, visibilité explicite et suppression possible.
Pas d’envoi automatique depuis le meter. Aucun import client ne prouve son authenticité.

Les agrégats devront séparer région, patch, boss, difficulté, taille du groupe et méthode
DPS. Exclure démos et imports non vérifiés des classements. Dédupliquer, limiter les
imports, contrôler les droits d’accès et montrer effectifs/distributions, pas seulement
une moyenne. Aucun paquet réseau, IP, token ou identifiant de compte dans les rapports.
