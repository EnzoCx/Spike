# DPSMeter 0.4.0 — détails à portée de souris

## Démarrer

Fermer uniquement l’ancienne fenêtre DPSMeter, puis lancer `DPSMeter-0.4.0.exe`.
Le jeu peut rester ouvert. L’historique et les réglages existants sont repris.
L’overlay s’ouvre automatiquement, sauf désactivation dans les réglages.
**Ctrl+Alt+M** permet de le masquer ou de le retrouver.

## Dans l’overlay

- **Survol d’un joueur** : classe, dégâts, DPS/HPS, contribution, durée, coups et
  critiques observés ; huit principales compétences avec quantités et pourcentages.
- **Clic sur un joueur** : toutes ses compétences. La flèche revient au classement.
- **Rapport complet** : ouvre le combat, le joueur et le même filtre dans le logiciel.
- **Boss / Tout** : choisit les dégâts au boss ou sur toutes les cibles.
- **En direct ▾** : dernier combat ou historique local. Un ancien combat reste figé
  pendant que la capture poursuit son travail.
- **···** : hauteur automatique, lignes compactes et opacité du fond. Les caractères
  restent opaques. Le redimensionnement manuel désactive la hauteur automatique.
- **◇** : verrouille et laisse passer les clics vers le jeu. Masquer puis rouvrir
  avec Ctrl+Alt+M pour le déverrouiller. Le survol nécessite un overlay déverrouillé.

Boss, durée, classement, classe, dégâts, critiques, contribution et DPS du groupe
sont directement lisibles. Les cinq lignes du groupe tiennent par défaut.
Les PV observés, la puissance reçue et le repère du personnage local apparaissent
quand le flux fournit ces informations. Aucune rétro-invention dans les anciennes sauvegardes.

## Analyse et historique

Le classement et les compétences sont côte à côte. Sélectionner un joueur conserve
le contexte du combat ; la recherche filtre ses compétences. Les détails présentent
les dégâts, coups, critiques, ticks, contribution, DPS, plus gros impact et impact moyen.
Déplier **Rythme du combat** pour la courbe du groupe ou du joueur sélectionné.

L’historique se recherche par boss ou nom de joueur, avec un filtre **Combats de boss**.
Durée, nombre de participants et DPS sont visibles avant d’ouvrir le rapport.
Français, anglais, espagnol et les trois thèmes s’appliquent aussi à l’overlay.

## Corrections vérifiées sur le combat d’Atiel fourni

Le filtre boss retire les dégâts infligés à d’autres cibles. La durée utilise le premier
et le dernier dégât des joueurs au boss. Les soins et l’attente de clôture ne diluent plus ce DPS.

| Joueur | DPS 0.4, arrondi | Capture NotMeter |
| --- | ---: | ---: |
| Joueur A | 13 797 | 13 797 |
| Joueur B | 11 440 | 11 440 |
| Joueur C | 10 466 | 10 466 |
| Joueur D | 9 921 | 9 921 |
| Joueur E | 6 457 | 6 457 |

Groupe : **52 082 DPS**, affiché **52,08 k** sur NotMeter. Durée source : 212 505 ms.
Les variantes de Coup au cœur sont maintenant réunies : **630 666 dégâts, 123 coups**.
Joueur A : **1 408 coups et 26,8 % de critiques observés**.
Les événements des fichiers d’origine n’ont pas été modifiés.

## Validation et limites

39 contrôles de calcul/stockage passent, ainsi que les régressions du protocole
(dégâts, critiques, soins, fragmentation, doublons, pause, LZ4). Compilation sans
avertissement. Vérifications d’interface et rendus hors écran pour les trois langues
et thèmes, plus rendus d’Atiel à taille normale et réduite. Aucun test n’affiche de
fenêtre au-dessus du jeu. Le véritable survol de souris et les clics traversants en
partie ne sont donc pas validés par une manipulation du bureau.

Cette comparaison est celle d’un combat précis. Les autres rencontres, flags et
frontières de durée ne sont pas tous validés face à NotMeter. Les critiques reçus
pour les autres joueurs peuvent être incomplets. La puissance et les PV nouvellement
exposés n’ont pas fait l’objet d’une nouvelle capture live pour cette livraison.

**Pas encore disponible :** uptime des buffs, coups de dos/de face, doubles et parfaits.
Le site communautaire n’est pas déployé. Npcap reste un prérequis déjà installé ;
il n’est pas redistribué. L’EXE embarque .NET. Aucune garantie CGU ni signature Windows.
Aucune donnée n’est envoyée en ligne et aucun réglage du jeu/réseau n’a été changé.
