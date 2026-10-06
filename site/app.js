const translations = {
  en: {
    navActivities: "Activities",
    activitiesEyebrow: "BETWEEN FIGHTS",
    activitiesTitle: "The right moment.\nYour next goal.",
    activitiesIntro: "Goals that expire. Resources that accumulate. Events coming up. Your session, together in Spike.",
    remindersTitle: "Ready for the next event.",
    remindersBody: "Discreet reminders at the top right, without interrupting combat. Several events? Cards stack and the rest wait their turn. Set lead time and sound in seconds.",
    activityIllustration: "Windows interface · demonstration data · click to enlarge",
    remindersNote: "Enable notifications in Activities → Events & reminders. Close the main window: Spike and its reminders stay in the system tray. Community schedules are editable; unconfirmed boss reminders are off by default.",
    checklistTitle: "Your routine, at your pace.",
    checklistBody: "Daily, Weekly, Reserves: everything in its place. Track Shugo keys without turning them into a daily chore. Click “i” to learn what each activity is and where to find it.",
    checklistNote: "One compact overlay header. Shugo and Rift countdowns beside the title, with the damage meter always available. Quick ±1 keys / ±40 Odyle controls; Wind Breeze purchases by character and server. Goals reset; reserves stay at your last manual reading.",
    activitySources: "Presets checked on 6 October 2026:",
    activitySourcesNote: "Global checklist cross-checked; counters and schedules remain adjustable to match your game.",
    eventsAlt: "Spike interface: compact settings and stacked reminders, demonstration data.",
    checklistAlt: "Spike interface: Shugo, Nightmare and Odyle reserves in the app and overlay, demonstration balances.",

    compareDate: 'As of 6 October 2026', compareLegend: 'Not confirmed: not documented in the sources', compareMethod: 'Sources and comparison methodology',
    navCompare: 'Compare', compareEyebrow: 'CHOOSE YOUR METER', compareTitle: 'Features, side by side.',
    compareIntro: 'Spike focuses on in-game readability and local reports, in French, English and Spanish. Here is what NotMeter, A2Tools and Abyss Logs offer too.',
    compareNote: 'Sources checked on 6 October 2026. Features documented or visible in official sources; the other apps have not been tested. “Not confirmed” does not mean absent. “Website” identifies a web feature, without assuming it is available in the desktop app.',
    compareScroll: 'On small screens, scroll the table horizontally to see all four tools.',
    compareCaption: 'Feature comparison of Spike, NotMeter, A2Tools and Abyss Logs', compareFeature: 'Feature',
    compareYes: 'Yes', compareNo: 'No', compareUnknown: 'Not confirmed', compareWeb: 'Website', compareNotAvailable: 'Not available',
    compareOverlay: 'Live DPS overlay', compareSkills: 'Damage by skill', compareHealing: 'Healing analysis', compareRawHealing: 'Raw healing and HPS',
    compareChecklist: 'Daily/weekly checklist & reserves', compareChecklistSpike: 'App and overlay',
    compareTimers: 'Shugo / Rift countdowns', compareTimersSpike: 'Compact overlay header',
    compareAlerts: 'Configurable event alerts', compareAlertsSpike: 'Stacked notifications, optional sound',
    compareHistory: 'Local fight history', compareBossSave: 'Boss fights auto-saved', compareLanguages: 'Documented languages',
    compareNotMeterLanguages: 'Website: 9, including FR / EN / ES', compareA2Languages: 'EN / KO / Traditional and Simplified Chinese', compareNineLanguages: '9 languages announced',
    compareSharing: 'Report sharing', compareSpikeSharing: 'Text copy / JSON v2, export without names', compareWebReports: 'Reports on the website', compareWebLinks: 'Web links',
    compareBuffs: 'Buff duration', compareBuffTimeline: 'Buff timeline', compareAdvanced: 'Back attacks, double and perfect hits',
    compareLeaderboards: 'Online leaderboards', compareUploads: 'Automatic fight uploads', compareOptIn: 'Optional, with consent',
    compareSources: 'Sources:', compareLanguageSource: 'website languages',
    compareLimits: 'This table compares features, not measurement accuracy or compliance with game rules. Report formats are not interchangeable: Spike only imports its own JSON v2. Its fights stay local, with no telemetry or automatic uploads.',
    skip: 'Skip to content', navPreview: 'Preview', navInstall: 'Installation', language: 'Language',
    eyebrow: 'COMBAT METER', headline1: 'Your combat,', headline2: 'clearly.',
    intro: 'Understand your fights. Track your goals. Be ready for the next event.',
    download: 'Download for Windows', downloadNote: 'Windows x64 · Free · No account', firstTime: 'First time? Read the installation guide',
    overlayLabel: 'THE MINI METER', demo: 'DEMO', overlayCaption: 'The essentials during combat.\nThe details, when you choose.',
    themeLegend: 'Same app. Your atmosphere.', dark: 'Graphite', light: 'Ivory', contrast: 'High contrast',
    specChecklist: 'Checklist & reserves', specEvents: 'Timers & alerts',
    spec1: 'Damage & healing', spec2: 'Discreet overlay', spec3: 'Local history', spec4: '3 languages · 3 themes',
    analysisEyebrow: 'AFTER THE IMPACT', analysisTitle: 'Every fight has\nsomething to tell you.', analysisIntro: 'From the first hit to the last, see what made the difference.',
    reportLabel: 'Fight report', reportCaption: 'Actual app preview · fictional data · click to enlarge',
    feature1Title: 'The right perspective.', feature1: 'DPS, healing and damage share. Switch between the boss and all targets to see the fight in context.',
    feature2Title: 'Beyond the ranking.', feature2: 'Select a player. Explore their skills, damage contribution and observed critical hits.',
    feature3Title: 'Your next improvement.', feature3: 'Review saved fights while capture continues. Copy a summary to share yourself.',
    quietEyebrow: 'THERE WHEN IT MATTERS', quietTitle: 'In your line of sight.\nOut of your way.', quietBody: 'Move the overlay, lock clicks and stay in control of your game. Out of combat, it fades, then collapses. The next fight brings it back.',
    shortcut: 'Show / hide', installEyebrow: 'YOUR NEXT FIGHT', installTitle: 'Three steps.\nThen you play.', installIntro: 'A standalone executable for Windows x64. No account to create, no .NET runtime to install.',
    release: 'See the latest release', step1Title: 'Download Spike.', step1: 'Open Spike.exe: only the overlay appears. Double-click its system tray icon to open the main window.', downloadFile: 'Download the executable ↓',
    step2Title: 'Set up Npcap, just once.', step2: 'If it is missing, Spike guides you. Close the game before installing Npcap from its official website, then return to check the installation.', npcap: 'Official Npcap website ↗',
    step3Title: 'Launch the game. Join the fight.', step3: 'Use windowed or borderless mode to see the overlay. With default settings, capture starts automatically.',
    faqEyebrow: 'GOOD TO KNOW', faqTitle: 'Before you jump in.',
    q1: 'Do my fights stay private?', a1: 'Fights and preferences stay on your PC. No telemetry or automatic fight uploads. Only the updater contacts GitHub to check for and download new versions.',
    q2: 'Is this an official tool?', a2: 'Spike is an independent project, with no NCSOFT affiliation or approval. It uses passive capture through Npcap, without injection or game-memory access. Compliance with the game’s rules is not guaranteed.',
    q3: 'Why does Windows show a warning?', a3: 'The executable does not yet have a publisher’s digital signature. Only download it from the official repository’s releases linked on this page.',
    q4: 'Are the results always complete?', a4: 'No. Names, HP or other players’ critical hits may be missing. Observed participants are not a confirmed party roster. Unidentified sources stay separate and their damage is retained. Game changes may require a decoder update.',
    q5: 'I already use DPSMeter. What happens to my history?', a5: 'Preferences, fights and JSON v2 files remain compatible. Download Spike.exe 0.5.2 manually once if you use an earlier version. Your DPSMeter data is copied on first launch, preserving the originals. Future updates download in the background; the arrow beside the version lets you retry. Installation happens on the next launch, with no forced restart.',
    closing: 'Your turn to play.', footerLine: 'Your combat, clearly. Your data, at home.', feedback: 'A problem or an idea? ↗',
    legal: 'Independent project. AION 2 and its artwork belong to NCSOFT. Code under the MIT license; third-party resources under their respective licenses.', credits: 'Credits and licenses',
    overlayAlt: 'Spike mini meter: ranking of five fictional characters.', reportAlt: 'Spike fictional fight report: damage, skill icons and duration.',
    description: 'Spike, the damage and healing meter for AION 2 Global. A discreet overlay, detailed reports and fights stored on your PC.'
  },
  es: {
    navActivities: "Actividades",
    activitiesEyebrow: "ENTRE COMBATES",
    activitiesTitle: "El momento justo.\nTu próximo objetivo.",
    activitiesIntro: "Objetivos que caducan. Recursos que se acumulan. Eventos que se acercan. Tu sesión, reunida en Spike.",
    remindersTitle: "Prepárate para el próximo evento.",
    remindersBody: "Avisos discretos arriba a la derecha, sin interrumpir el combate. ¿Varios eventos? Las tarjetas se apilan y el resto espera su turno. Ajusta antelación y sonido en segundos.",
    activityIllustration: "Interfaz Windows · datos de demostración · pulsa para ampliar",
    remindersNote: "Activa los avisos en Actividades → Eventos y avisos. Cierra la ventana principal: Spike y sus avisos siguen en la bandeja del sistema. Horarios comunitarios editables; avisos de jefes sin confirmar desactivados por defecto.",
    checklistTitle: "Tu rutina, a tu ritmo.",
    checklistBody: "Diario, Semana, Reservas: cada actividad en su sitio. Sigue tus llaves Shugo sin convertirlas en una obligación diaria. Pulsa «i» para saber qué hacer y dónde encontrarlo.",
    checklistNote: "Overlay compacto: cuentas atrás junto al título, sin sustituir el medidor. Controles ±1 llaves / ±40 Odyle; compras por personaje y servidor. Los objetivos se reinician; las reservas conservan tu último registro manual.",
    activitySources: "Preajustes consultados el 6 de octubre de 2026:",
    activitySourcesNote: "Lista Global contrastada; contadores y horarios ajustables según tu juego.",
    eventsAlt: "Interfaz Spike: configuración compacta y avisos apilados, datos de demostración.",
    checklistAlt: "Interfaz Spike: reservas de Shugo, Pesadilla y Odyle en la aplicación y el overlay, saldos de demostración.",

    compareDate: 'A 6 de octubre de 2026', compareLegend: 'Sin confirmar: información no documentada', compareMethod: 'Fuentes y método de comparación',
    navCompare: 'Comparativa', compareEyebrow: 'ELIGE TU MEDIDOR', compareTitle: 'Las funciones, lado a lado.',
    compareIntro: 'Spike se centra en la legibilidad durante el juego y los informes locales, en francés, inglés y español. Esto es lo que también ofrecen NotMeter, A2Tools y Abyss Logs.',
    compareNote: 'Fuentes consultadas el 6 de octubre de 2026. Funciones documentadas o visibles en fuentes oficiales; no se han probado las otras aplicaciones. «Sin confirmar» no significa ausente. «Sitio web» identifica una función de la web, sin asumir que exista en la aplicación.',
    compareScroll: 'En pantallas pequeñas, desplaza la tabla horizontalmente para ver las cuatro herramientas.',
    compareCaption: 'Comparación de funciones de Spike, NotMeter, A2Tools y Abyss Logs', compareFeature: 'Función',
    compareYes: 'Sí', compareNo: 'No', compareUnknown: 'Sin confirmar', compareWeb: 'Sitio web', compareNotAvailable: 'No disponible',
    compareOverlay: 'Overlay de DPS en directo', compareSkills: 'Daño por habilidad', compareHealing: 'Análisis de sanación', compareRawHealing: 'Sanación bruta y HPS',
    compareChecklist: 'Lista diaria/semanal y reservas', compareChecklistSpike: 'Aplicación y overlay',
    compareTimers: 'Cuentas atrás Shugo / Rift', compareTimersSpike: 'Cabecera compacta del overlay',
    compareAlerts: 'Avisos de eventos configurables', compareAlertsSpike: 'Avisos apilados, sonido opcional',
    compareHistory: 'Historial local', compareBossSave: 'Guardado automático de jefes', compareLanguages: 'Idiomas documentados',
    compareNotMeterLanguages: 'Sitio web: 9, incluidos FR / EN / ES', compareA2Languages: 'EN / KO / chino tradicional y simplificado', compareNineLanguages: '9 idiomas anunciados',
    compareSharing: 'Compartir informes', compareSpikeSharing: 'Copiar texto / JSON v2, exportar sin nombres', compareWebReports: 'Informes en el sitio web', compareWebLinks: 'Enlaces web',
    compareBuffs: 'Duración de buffs', compareBuffTimeline: 'Cronología de buffs', compareAdvanced: 'Ataques por la espalda, golpes dobles y perfectos',
    compareLeaderboards: 'Clasificaciones en línea', compareUploads: 'Envío automático de combates', compareOptIn: 'Opcional, con consentimiento',
    compareSources: 'Fuentes:', compareLanguageSource: 'idiomas del sitio',
    compareLimits: 'Esta tabla compara funciones, no la precisión de las medidas ni el cumplimiento de las reglas del juego. Los formatos no son intercambiables: Spike solo importa su propio JSON v2. Sus combates se guardan localmente, sin telemetría ni envíos automáticos.',
    skip: 'Ir al contenido', navPreview: 'Vista previa', navInstall: 'Instalación', language: 'Idioma',
    eyebrow: 'MEDIDOR DE COMBATE', headline1: 'Tu combate,', headline2: 'claro.',
    intro: 'Entiende tus combates. Sigue tus objetivos. Prepárate para el próximo evento.',
    download: 'Descargar para Windows', downloadNote: 'Windows x64 · Gratis · Sin cuenta', firstTime: '¿Primera vez? Guía de instalación',
    overlayLabel: 'EL MINIMEDIDOR', demo: 'DEMO', overlayCaption: 'Lo esencial durante el combate.\nLos detalles, cuando tú decidas.',
    themeLegend: 'La misma app. Tu ambiente.', dark: 'Grafito', light: 'Marfil', contrast: 'Alto contraste',
    specChecklist: 'Lista y reservas', specEvents: 'Temporizadores y avisos',
    spec1: 'Daño y sanación', spec2: 'Overlay discreto', spec3: 'Historial local', spec4: '3 idiomas · 3 temas',
    analysisEyebrow: 'TRAS EL IMPACTO', analysisTitle: 'Cada combate tiene\nalgo que contarte.', analysisIntro: 'Del primer golpe al último, descubre qué marcó la diferencia.',
    reportLabel: 'Informe de combate', reportCaption: 'Vista de la aplicación · datos ficticios · pulsa para ampliar',
    feature1Title: 'La perspectiva adecuada.', feature1: 'DPS, sanación y porcentaje del daño. Alterna entre el jefe y todos los objetivos para entender el combate en su contexto.',
    feature2Title: 'Más allá de la clasificación.', feature2: 'Selecciona un jugador. Explora sus habilidades, su daño y los golpes críticos observados.',
    feature3Title: 'Tu próxima mejora.', feature3: 'Revisa combates guardados mientras la captura continúa. Copia un resumen para compartirlo tú mismo.',
    quietEyebrow: 'PRESENTE CUANDO IMPORTA', quietTitle: 'A la vista.\nSin estorbar.', quietBody: 'Mueve el overlay, bloquea los clics y mantén el control del juego. Fuera de combate, se atenúa y luego se reduce. El siguiente combate lo restaura.',
    shortcut: 'Mostrar / ocultar', installEyebrow: 'TU PRÓXIMO COMBATE', installTitle: 'Tres pasos.\nY a jugar.', installIntro: 'Un ejecutable autónomo para Windows x64. Sin crear una cuenta ni instalar el runtime de .NET.',
    release: 'Ver la última versión', step1Title: 'Descarga Spike.', step1: 'Abre Spike.exe: solo aparece el overlay. Haz doble clic en su icono de la bandeja para abrir la ventana principal.', downloadFile: 'Descargar el ejecutable ↓',
    step2Title: 'Prepara Npcap, solo una vez.', step2: 'Si falta, Spike te guía. Cierra el juego antes de instalar Npcap desde su web oficial y vuelve para comprobar la instalación.', npcap: 'Web oficial de Npcap ↗',
    step3Title: 'Abre el juego. Entra en combate.', step3: 'Usa el modo ventana o sin bordes para ver el overlay. Con los ajustes predeterminados, la captura comienza automáticamente.',
    faqEyebrow: 'CONVIENE SABERLO', faqTitle: 'Antes de empezar.',
    q1: '¿Mis combates son privados?', a1: 'Tus combates y preferencias se quedan en tu PC. Sin telemetría ni envíos automáticos de combates. Solo el actualizador contacta con GitHub para buscar y descargar nuevas versiones.',
    q2: '¿Es una herramienta oficial?', a2: 'Spike es un proyecto independiente, sin afiliación ni aprobación de NCSOFT. Utiliza captura pasiva mediante Npcap, sin inyección ni acceso a la memoria del juego. No se garantiza el cumplimiento de las reglas del juego.',
    q3: '¿Por qué Windows muestra una advertencia?', a3: 'El ejecutable aún no tiene firma digital de editor. Descárgalo únicamente de las versiones del repositorio oficial enlazado en esta página.',
    q4: '¿Los resultados siempre están completos?', a4: 'No. Pueden faltar nombres, PV o críticos de otros jugadores. Los participantes observados no son un grupo confirmado. Las fuentes sin identificar se mantienen separadas y su daño se conserva. Los cambios del juego pueden requerir actualizar el decodificador.',
    q5: 'Ya uso DPSMeter. ¿Qué ocurre con mi historial?', a5: 'Tus preferencias, combates y archivos JSON v2 siguen siendo compatibles. Descarga Spike.exe 0.5.2 manualmente una vez si usas una versión anterior. Tus datos de DPSMeter se copian al primer inicio, conservando los originales. Las siguientes actualizaciones se descargan en segundo plano; la flecha junto a la versión permite reintentar. Se instalan al volver a abrir la app, sin reinicio forzado.',
    closing: 'Te toca jugar.', footerLine: 'Tu combate, claro. Tus datos, en casa.', feedback: '¿Un problema o una idea? ↗',
    legal: 'Proyecto independiente. AION 2 y sus imágenes pertenecen a NCSOFT. Código bajo licencia MIT; recursos de terceros bajo sus respectivas licencias.', credits: 'Créditos y licencias',
    overlayAlt: 'Minimedidor Spike: clasificación de cinco personajes ficticios.', reportAlt: 'Informe Spike de un combate ficticio: daño, iconos de habilidades y duración.',
    description: 'Spike, el medidor de daño y sanación para AION 2 Global. Un overlay discreto, informes detallados y combates guardados en tu PC.'
  }
};

const textElements = [...document.querySelectorAll('[data-i18n]')];
const french = Object.fromEntries(textElements.map(element => {
  const copy = element.cloneNode(true);
  copy.querySelectorAll('br').forEach(lineBreak => lineBreak.replaceWith('\n'));
  return [element.dataset.i18n, copy.textContent];
}));
translations.fr = { ...french, overlayAlt: document.querySelector('#overlay-preview').alt, reportAlt: document.querySelector('#report-preview').alt, description: document.querySelector('meta[name="description"]').content, eventsAlt: document.querySelector('#events-preview').alt, checklistAlt: document.querySelector('#checklist-preview').alt };
const languages = ['fr', 'en', 'es'];
const themes = ['dark', 'light', 'contrast'];
const readPreference = key => { try { return localStorage.getItem(key); } catch { return null; } };
const savePreference = (key, value) => { try { localStorage.setItem(key, value); } catch { /* Preferences remain usable for this visit. */ } };
const url = new URL(location.href);
let language = [url.searchParams.get('lang'), readPreference('spike-language'), navigator.language.slice(0, 2), 'fr'].find(value => languages.includes(value));
let theme = [readPreference('spike-theme'), 'dark'].find(value => themes.includes(value));

function updatePreviews() {
  const copy = translations[language];
  for (const view of ['events', 'checklist']) {
    document.querySelector(`#${view}-preview`).src = `assets/${view}-en-${theme}.png?v=activities-4`;
    document.querySelector(`#${view}-preview`).alt = copy[`${view}Alt`];
    document.querySelector(`#${view}-link`).href = `assets/${view}-en-${theme}.png?v=activities-4`;
  }
  document.querySelector('#overlay-preview').src = `assets/overlay-en-${theme}.png?v=0.5.15`;
  document.querySelector('#overlay-preview').alt = copy.overlayAlt;
  document.querySelector('#report-preview').src = `assets/report-en-${theme}.png?v=0.5.15`;
  document.querySelector('#report-preview').alt = copy.reportAlt;
  document.querySelector('#report-link').href = `assets/report-en-${theme}.png?v=0.5.15`;
}

function setLanguage(value) {
  if (!languages.includes(value)) return;
  language = value;
  const copy = translations[language];
  textElements.forEach(element => { element.textContent = copy[element.dataset.i18n]; });
  document.documentElement.lang = language;
  document.querySelector('#language').value = language;
  document.title = `Spike — ${copy.headline1} ${copy.headline2}`;
  document.querySelector('meta[name="description"]').content = copy.description;
  document.querySelector('meta[property="og:title"]').content = document.title;
  document.querySelector('meta[property="og:description"]').content = copy.description;
  savePreference('spike-language', language);
  updatePreviews();
}

function setTheme(value) {
  if (!themes.includes(value)) return;
  theme = value;
  document.documentElement.dataset.theme = theme;
  document.querySelectorAll('[data-theme-choice]').forEach(button => button.setAttribute('aria-pressed', String(button.dataset.themeChoice === theme)));
  document.querySelectorAll('.wordmark img').forEach(img => { img.src = `assets/symbol-${theme === 'light' ? 'light' : 'dark'}.svg`; });
  document.querySelector('meta[name="theme-color"]').content = getComputedStyle(document.documentElement).getPropertyValue('--color-background').trim();
  savePreference('spike-theme', theme);
  updatePreviews();
}

document.querySelector('#language').addEventListener('change', event => {
  setLanguage(event.target.value);
  url.searchParams.set('lang', language);
  url.hash = location.hash;
  history.replaceState(null, '', url);
});
document.querySelectorAll('[data-theme-choice]').forEach(button => button.addEventListener('click', () => setTheme(button.dataset.themeChoice)));
setLanguage(language);
setTheme(theme);
document.querySelector('.language').hidden = false;
document.querySelector('.themes').hidden = false;
