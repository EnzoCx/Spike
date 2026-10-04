# 0.4.9 — fin de combat en monde ouvert

- Les joueurs qui farment autour ne maintiennent plus indéfiniment le combat actif
  lorsque le personnage local est identifié.
- Clôture après 12 secondes sans dégâts personnels infligés/reçus ni soins directs
  vers autrui ; un nouveau combat attend une nouvelle activité personnelle.
- Les invocations au propriétaire connu comptent pour leur joueur. Les soins reçus,
  personnels et périodiques ne relancent pas le délai.
- Les événements des autres joueurs et des sources non identifiées restent conservés
  pendant le segment actif. Aucun changement du format des archives ou des préférences.

Limites : sans personnage identifié, le mode d’observation garde le délai global.
Une mort ou une pause personnelle de 12 secondes peut découper un combat même si les
alliés continuent. Le classement des joueurs observés n’est pas un groupe confirmé.

Validation : scénarios synthétiques hors réseau (farm continu, reprise, soins,
invocations, identification tardive, conservation des événements et fenêtres de dégâts),
contrôles Core/protocole, mises à jour et interface hors écran via `tools/Verify.ps1`.
Pas de capture en jeu pendant ces vérifications.
