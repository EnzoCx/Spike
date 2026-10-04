# 0.4.12 — un parse par tentative de boss

- Le combat continue tant que le boss engagé reçoit ou inflige des dégâts, même si
  le personnage local est mort ou inactif. Le farm sur les autres cibles ne suffit pas.
- Après une phase silencieuse, la reprise complète le même parse et la même archive,
  avec les dégâts des participants et des sources non identifiées conservés.
- Un reset des PV observé après wipe ouvre une nouvelle tentative, même si le boss
  conserve son identifiant et si la reprise est rapide. Des PV à zéro scellent le combat.
- Un autre boss engagé est distingué par ses identifiants, même s’il porte le même nom.
- Les soigneurs peuvent engager le boss via le combat observé d’un joueur qu’ils soignent.
  Les adds et soins personnels pendant les phases restent dans le parse.
- Terminer reste disponible au repos pour forcer une nouvelle tentative. Une pause,
  un changement de zone/personnage ou un redémarrage empêchent la reprise de l’ancien parse.

Les archives v2 et les préférences restent compatibles. Le DPS du boss couvre ses
premiers à derniers dégâts sortants, sans ajouter le délai de clôture.

Limites : la séparation dépend des signaux reçus. Le reset correspond à un retour
au maximum de PV observé après une baisse sous 95 % ; un reset manquant ou une mort
non reçue peut fusionner des tentatives, et une mécanique de remontée complète des PV
peut ressembler à un reset. Le bouton Terminer permet de séparer manuellement.
Les boss non reconnus gardent la règle d’inactivité personnelle de 12 secondes.

Validation : scénarios synthétiques hors réseau pour la continuité, les phases,
le farm voisin, les resets dans un même lot, la mort, les identifiants réutilisés,
les soigneurs et la sauvegarde sans doublon ; `tools/Verify.ps1` pour les contrôles
Core/protocole, mises à jour et interface FR/EN/ES dans les trois thèmes.
Aucune capture en jeu ni interaction avec le meter ouvert.
