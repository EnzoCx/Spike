# DPSMeter 0.4.2 — un overlay plus facile à placer

- **Aimantation aux quatre bords** pendant le déplacement, activée par défaut.
  Le bord pris en compte est celui de l’écran concerné, hors barre des tâches.
- **Maj + glisser** pour ignorer temporairement l’aimantation. Le menu **···**
  permet aussi de la désactiver durablement.
- **Clic droit sur l’en-tête** pour ouvrir les options.
- **Double-clic sur l’en-tête** pour basculer entre lignes normales et compactes.
- **··· → Placer sur cet écran** : quatre coins ou centre, en un clic.
- **Réglages → Recentrer l’overlay** : le ramène sur l’écran de la fenêtre principale
  et le déverrouille, sans perdre le combat sélectionné ni arrêter la capture.
- **Écran débranché ou résolution modifiée** : repositionnement dans une zone accessible.
- **Ancrage en bas** conservé quand la hauteur automatique augmente ou diminue.
- Le redimensionnement manuel désactive la hauteur automatique dès le début du geste,
  afin qu’une actualisation de combat ne modifie pas la taille sous la souris.

Les anciens réglages sont compatibles. L’aimantation est enregistrée avec les autres
préférences. Les gestes et menus sont expliqués en français, anglais et espagnol.

## Vérifications

16 contrôles de placement : quatre bords, coins, absence d’attraction à distance,
déplacement libre, écran à coordonnées négatives, seuil à une échelle de 200 %,
récupération d’écran absent et maintien du bord inférieur. Le branchement Windows
a aussi été testé avec une véritable fenêtre native **invisible**, sans déplacement
du curseur, clic simulé ou changement d’affichage du PC. Les configurations multiples
et échelles différentes sont simulées ; aucun déplacement manuel en partie n’a été effectué.

Les 39 contrôles existants et les vérifications d’interface complètent ces essais.

Implémentation fondée sur [WM_MOVING](https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-moving),
[MonitorFromRect](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-monitorfromrect)
et [GetDpiForWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getdpiforwindow).

Fermer uniquement l’ancienne fenêtre DPSMeter, puis lancer **DPSMeter-0.4.2.exe**.
Le jeu et l’historique restent en place.
