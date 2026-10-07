# Spike 0.5.17

- Le compteur enregistre désormais uniquement les combats contre les boss reconnus. Le farm sur les petits monstres ne démarre plus de combat et ne remplit plus l’historique.
- Les dégâts sur les petits monstres sont aussi exclus pendant un boss et entre ses phases. Ils ne prolongent ni ne réactivent son combat.
- Les soins seuls hors activité du boss ne prolongent ni ne réactivent le compteur.
- Les soins pendant le combat de boss, les dégâts des sources non identifiées et la reprise des phases restent pris en compte. Les anciennes archives restent consultables.
- Les messages d’attente précisent ce fonctionnement en français, anglais et espagnol.

La reconnaissance dépend du catalogue et des annonces reçues du jeu. Les premiers dégâts peuvent être récupérés si le boss est identifié dans les 30 secondes, avec un tampon limité à 4 096 événements ; un boss non reconnu n’est pas enregistré.
