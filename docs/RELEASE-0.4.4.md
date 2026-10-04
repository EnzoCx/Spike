# DPSMeter 0.4.4 — joueurs et sources non identifiées

Le rapport d’Atiel contenait des lignes anonymes ne lançant que Mur de feu,
Tempête glaciale ou Piège explosif. Une classe déduite d’une compétence ne
suffit pas à prouver qu’une source est un joueur.

- Ces sources sont regroupées dans une ligne dépliable **Sources à identifier**,
  après les joueurs. Leurs dégâts restent inclus dans le total du groupe.
- Déplier cette ligne pour consulter chaque source, puis cliquer ou survoler
  pour voir ses compétences. Le rapport complet les distingue également.
- Elles ne gonflent plus le nombre de joueurs affiché ni celui de l’historique.
- Un joueur sans nom utilisant d’autres compétences reste présent, notamment
  l’aède « Player #5483 » du combat examiné.
- Les anciens rapports locaux sont classés à la lecture, sans réécriture.

Le propriétaire de ces sources reste inconnu : les sauvegardes ne permettent
pas de le prouver. Aucun dégât n’est attribué sur la seule base d’une classe,
supprimé ou ajouté. Ce correctif ne garantit pas encore un groupe exact dans
toutes les situations ni l’identification de tous les effets du jeu.

Vérifié avec le combat enregistré, 50 contrôles de calcul/historique,
les régressions protocole et les rendus/interactions hors écran. Aucun arrêt
du jeu ou de l’instance DPSMeter déjà ouverte.

Fermer uniquement l’ancien DPSMeter, puis lancer **DPSMeter-0.4.4.exe**.
