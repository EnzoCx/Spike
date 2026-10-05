# rDPS bêta

Dans le rapport, choisir **rDPS bêta** ; dans l’overlay, **rDPS β**.
Le DPS brut reste le mode initial. Les survols et détails montrent les contributions
estimées reçues et apportées. Le bouton Copier conserve les mentions bêta/partielle.

Cette version est une expérience utilisable, **pas un rDPS fiable ou complet**.
Elle n’a pas été testée en jeu avant publication : maintenance en cours.

## Modèle provisoire

Deux familles uniquement : Light of Protection (clerc, 17410000) et Undefeated Mantra
(aède, 18190000). Le modèle `auras-level1-v1` suppose le bonus nominal de niveau 1,
10,5 %, appliqué comme multiplicateur final. Il ne connaît pas le niveau réel, les
statistiques personnelles ni les spécialisations. Cette hypothèse peut surestimer
ou sous-estimer le bonus ; elle n’est pas une formule Global établie.

Pour un coup `D`, le bonus externe est `arrondi(D × 105 / 1105)` (au pair).
Exemple : 1 105 dégâts deviennent 1 000 personnels et 105 attribués au support.
Le rDPS partiel est `(dégâts bruts − bonus reçus + bonus apportés) / durée`.
Les auras personnelles ne transfèrent rien. La somme des dégâts reste identique,
avec la fenêtre du boss existante (premier au dernier coup, minimum une seconde).

Seule une aura observée, active selon sa durée annoncée et dont l’auteur est
explicitement identifié est exploitable. Deux auras concurrentes sont ambiguës :
aucune priorité de niveau n’est inventée. Les invocations ne reçoivent pas les auras
de leur propriétaire par supposition. Les changements de contexte, réutilisations
d’ID, pauses et désynchronisations détectées invalident les observations concernées.
Les retraits anticipés, sorties de portée silencieuses, autres buffs/débuffs,
modificateurs de critique/double et pertes de trames indétectables restent des limites.

## Lire le résultat

- **≈ / estimation partielle** : contribution calculée avec ce modèle provisoire.
- **—** : aucune aura attribuable pour ce participant, ou anciennes données absentes.
  Cela ne signifie pas que le joueur n’avait aucun buff.
- **Dégâts avec aura observée** : part des dégâts associés à une aura exploitable.
  Même 100 % ne mesure ni la précision ni la couverture de tous les buffs.
- Les coups sans métadonnées gardent leurs dégâts bruts dans le calcul partiel.
  Un résultat partiel ne permet donc pas de comparer équitablement les classes.
- Compétences et courbe : DPS brut ; pas de ventilation par compétence du bonus apporté.

Les nouvelles archives JSON v2 conservent le modèle et le bonus figé par coup.
Les anciennes restent lisibles, sans inventer rétrospectivement les auras manquantes.
Aucune trame brute n’est enregistrée, aucune donnée n’est envoyée automatiquement.

## Premier essai après la maintenance

1. Lancer la nouvelle version de Spike avant le combat et sélectionner rDPS β.
2. Avec un clerc ou un aède identifié, activer l’une des deux auras, puis infliger
   quelques séries de coups sur la même cible. Noter le niveau réel de l’aura.
3. Vérifier les lignes du support et de l’attaquant, puis le détail des bonus et le
   pourcentage de dégâts associés à une aura. Comparer au mode DPS brut.
4. Désactiver l’aura ou sortir de portée et observer si l’estimation persiste.
   Ce cas sert à vérifier les limites connues du cycle de vie.
5. Rapporter la version, la classe, le nom/niveau de l’aura, les valeurs affichées
   et le comportement constaté. Une capture d’écran ou un export volontaire peut
   aider ; vérifier son contenu avant de le partager.

Un premier essai valide l’intégration, pas la précision du modèle. Le protocole de
calibration et les sources sont dans [RDPS-RESEARCH.md](RDPS-RESEARCH.md).
