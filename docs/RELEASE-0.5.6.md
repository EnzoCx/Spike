# Spike 0.5.6

- Correction du décrochage lent de l’overlay : les petits déplacements du pointeur s’accumulent désormais, même lorsque la fenêtre est aimantée à un bord ou dans un coin.
- L’aimantation conserve sa distance de 6 pixels logiques et le contournement avec Maj. La fenêtre peut toujours se raccrocher en revenant vers un bord.

Validation : contrôles hors ligne complets et 51 contrôles de placement, dont des déplacements successifs d’un pixel sur les quatre bords et dans les quatre coins. Le test reproduit le blocage lorsque la correction est retirée. Vérifications sur une fenêtre native invisible, sans déplacement du pointeur ni capture live.
