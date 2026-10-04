const translations = {
  en: {
    skip: 'Skip to content', navPreview: 'Preview', navInstall: 'Installation', language: 'Language',
    eyebrow: 'COMBAT METER', headline1: 'Your combat,', headline2: 'clearly.',
    intro: 'Keep an eye on your damage. Understand every skill. Revisit your fights, at your own pace.',
    download: 'Download for Windows', downloadNote: 'Windows x64 · Free · No account', firstTime: 'First time? Read the installation guide',
    overlayLabel: 'THE MINI METER', demo: 'DEMO', overlayCaption: 'The essentials during combat.\nThe details, when you choose.',
    themeLegend: 'Same app. Your atmosphere.', dark: 'Graphite', light: 'Ivory', contrast: 'High contrast',
    spec1: 'Damage & healing', spec2: 'Discreet overlay', spec3: 'Local history', spec4: '3 languages · 3 themes',
    analysisEyebrow: 'AFTER THE IMPACT', analysisTitle: 'Every fight has\nsomething to tell you.', analysisIntro: 'From the first hit to the last, see what made the difference.',
    reportLabel: 'Fight report', reportCaption: 'Actual app preview · fictional data · click to enlarge',
    feature1Title: 'The right perspective.', feature1: 'DPS, healing and damage share. Switch between the boss and all targets to see the fight in context.',
    feature2Title: 'Beyond the ranking.', feature2: 'Select a player. Explore their skills, damage contribution and observed critical hits.',
    feature3Title: 'Your next improvement.', feature3: 'Review saved fights while capture continues. Copy a summary to share yourself.',
    quietEyebrow: 'THERE WHEN IT MATTERS', quietTitle: 'In your line of sight.\nOut of your way.', quietBody: 'Move the overlay, lock clicks and stay in control of your game. Out of combat, it fades, then collapses. The next fight brings it back.',
    shortcut: 'Show / hide', installEyebrow: 'YOUR NEXT FIGHT', installTitle: 'Three steps.\nThen you play.', installIntro: 'A standalone executable for Windows x64. No account to create, no .NET runtime to install.',
    release: 'See the latest release', step1Title: 'Download Spike.', step1: 'Save DPSMeter.exe in a personal folder, then open it. This is Spike’s filename, kept for update compatibility.', downloadFile: 'Download the executable ↓',
    step2Title: 'Set up Npcap, just once.', step2: 'If it is missing, Spike guides you. Close the game before installing Npcap from its official website, then return to check the installation.', npcap: 'Official Npcap website ↗',
    step3Title: 'Launch the game. Join the fight.', step3: 'Use windowed or borderless mode to see the overlay. With default settings, capture starts automatically.',
    faqEyebrow: 'GOOD TO KNOW', faqTitle: 'Before you jump in.',
    q1: 'Do my fights stay private?', a1: 'Fights and preferences stay on your PC. No telemetry or automatic fight uploads. Only the updater contacts GitHub to check for and download new versions.',
    q2: 'Is this an official tool?', a2: 'Spike is an independent project, with no NCSOFT affiliation or approval. It uses passive capture through Npcap, without injection or game-memory access. Compliance with the game’s rules is not guaranteed.',
    q3: 'Why does Windows show a warning?', a3: 'The executable does not yet have a publisher’s digital signature. Only download it from the official repository’s releases linked on this page.',
    q4: 'Are the results always complete?', a4: 'No. Names, HP or other players’ critical hits may be missing. Observed participants are not a confirmed party roster. Unidentified sources stay separate and their damage is retained. Game changes may require a decoder update.',
    q5: 'I already use DPSMeter. What happens to my history?', a5: 'Spike is the new name for DPSMeter. Your preferences, fights and JSON v2 files remain compatible. From DPSMeter 0.4.6 onward, updates download in the background and apply on the next launch. No restart is forced.',
    closing: 'Your turn to play.', footerLine: 'Your combat, clearly. Your data, at home.', feedback: 'A problem or an idea? ↗',
    legal: 'Independent project. AION 2 and its artwork belong to NCSOFT. Code under the MIT license; third-party resources under their respective licenses.', credits: 'Credits and licenses',
    overlayAlt: 'Spike mini meter: ranking of four fictional characters.', reportAlt: 'Spike fictional fight report: damage, skills and duration.',
    description: 'Spike, the damage and healing meter for AION 2 Global. A discreet overlay, detailed reports and fights stored on your PC.'
  },
  es: {
    skip: 'Ir al contenido', navPreview: 'Vista previa', navInstall: 'Instalación', language: 'Idioma',
    eyebrow: 'MEDIDOR DE COMBATE', headline1: 'Tu combate,', headline2: 'claro.',
    intro: 'Sigue tu daño. Entiende cada habilidad. Revisa tus combates, a tu ritmo.',
    download: 'Descargar para Windows', downloadNote: 'Windows x64 · Gratis · Sin cuenta', firstTime: '¿Primera vez? Guía de instalación',
    overlayLabel: 'EL MINIMEDIDOR', demo: 'DEMO', overlayCaption: 'Lo esencial durante el combate.\nLos detalles, cuando tú decidas.',
    themeLegend: 'La misma app. Tu ambiente.', dark: 'Grafito', light: 'Marfil', contrast: 'Alto contraste',
    spec1: 'Daño y sanación', spec2: 'Overlay discreto', spec3: 'Historial local', spec4: '3 idiomas · 3 temas',
    analysisEyebrow: 'TRAS EL IMPACTO', analysisTitle: 'Cada combate tiene\nalgo que contarte.', analysisIntro: 'Del primer golpe al último, descubre qué marcó la diferencia.',
    reportLabel: 'Informe de combate', reportCaption: 'Vista de la aplicación · datos ficticios · pulsa para ampliar',
    feature1Title: 'La perspectiva adecuada.', feature1: 'DPS, sanación y porcentaje del daño. Alterna entre el jefe y todos los objetivos para entender el combate en su contexto.',
    feature2Title: 'Más allá de la clasificación.', feature2: 'Selecciona un jugador. Explora sus habilidades, su daño y los golpes críticos observados.',
    feature3Title: 'Tu próxima mejora.', feature3: 'Revisa combates guardados mientras la captura continúa. Copia un resumen para compartirlo tú mismo.',
    quietEyebrow: 'PRESENTE CUANDO IMPORTA', quietTitle: 'A la vista.\nSin estorbar.', quietBody: 'Mueve el overlay, bloquea los clics y mantén el control del juego. Fuera de combate, se atenúa y luego se reduce. El siguiente combate lo restaura.',
    shortcut: 'Mostrar / ocultar', installEyebrow: 'TU PRÓXIMO COMBATE', installTitle: 'Tres pasos.\nY a jugar.', installIntro: 'Un ejecutable autónomo para Windows x64. Sin crear una cuenta ni instalar el runtime de .NET.',
    release: 'Ver la última versión', step1Title: 'Descarga Spike.', step1: 'Guarda DPSMeter.exe en una carpeta personal y ábrelo. Es el nombre del archivo de Spike, conservado para las actualizaciones.', downloadFile: 'Descargar el ejecutable ↓',
    step2Title: 'Prepara Npcap, solo una vez.', step2: 'Si falta, Spike te guía. Cierra el juego antes de instalar Npcap desde su web oficial y vuelve para comprobar la instalación.', npcap: 'Web oficial de Npcap ↗',
    step3Title: 'Abre el juego. Entra en combate.', step3: 'Usa el modo ventana o sin bordes para ver el overlay. Con los ajustes predeterminados, la captura comienza automáticamente.',
    faqEyebrow: 'CONVIENE SABERLO', faqTitle: 'Antes de empezar.',
    q1: '¿Mis combates son privados?', a1: 'Tus combates y preferencias se quedan en tu PC. Sin telemetría ni envíos automáticos de combates. Solo el actualizador contacta con GitHub para buscar y descargar nuevas versiones.',
    q2: '¿Es una herramienta oficial?', a2: 'Spike es un proyecto independiente, sin afiliación ni aprobación de NCSOFT. Utiliza captura pasiva mediante Npcap, sin inyección ni acceso a la memoria del juego. No se garantiza el cumplimiento de las reglas del juego.',
    q3: '¿Por qué Windows muestra una advertencia?', a3: 'El ejecutable aún no tiene firma digital de editor. Descárgalo únicamente de las versiones del repositorio oficial enlazado en esta página.',
    q4: '¿Los resultados siempre están completos?', a4: 'No. Pueden faltar nombres, PV o críticos de otros jugadores. Los participantes observados no son un grupo confirmado. Las fuentes sin identificar se mantienen separadas y su daño se conserva. Los cambios del juego pueden requerir actualizar el decodificador.',
    q5: 'Ya uso DPSMeter. ¿Qué ocurre con mi historial?', a5: 'Spike es el nuevo nombre de DPSMeter. Tus preferencias, combates y archivos JSON v2 siguen siendo compatibles. Desde DPSMeter 0.4.6, las actualizaciones se descargan en segundo plano y se aplican al volver a abrir la app. No se fuerza ningún reinicio.',
    closing: 'Te toca jugar.', footerLine: 'Tu combate, claro. Tus datos, en casa.', feedback: '¿Un problema o una idea? ↗',
    legal: 'Proyecto independiente. AION 2 y sus imágenes pertenecen a NCSOFT. Código bajo licencia MIT; recursos de terceros bajo sus respectivas licencias.', credits: 'Créditos y licencias',
    overlayAlt: 'Minimedidor Spike: clasificación de cuatro personajes ficticios.', reportAlt: 'Informe Spike de un combate ficticio: daño, habilidades y duración.',
    description: 'Spike, el medidor de daño y sanación para AION 2 Global. Un overlay discreto, informes detallados y combates guardados en tu PC.'
  }
};

const textElements = [...document.querySelectorAll('[data-i18n]')];
const french = Object.fromEntries(textElements.map(element => {
  const copy = element.cloneNode(true);
  copy.querySelectorAll('br').forEach(lineBreak => lineBreak.replaceWith('\n'));
  return [element.dataset.i18n, copy.textContent];
}));
translations.fr = { ...french, overlayAlt: document.querySelector('#overlay-preview').alt, reportAlt: document.querySelector('#report-preview').alt, description: document.querySelector('meta[name="description"]').content };
const languages = ['fr', 'en', 'es'];
const themes = ['dark', 'light', 'contrast'];
const readPreference = key => { try { return localStorage.getItem(key); } catch { return null; } };
const savePreference = (key, value) => { try { localStorage.setItem(key, value); } catch { /* Preferences remain usable for this visit. */ } };
const url = new URL(location.href);
let language = [url.searchParams.get('lang'), readPreference('spike-language'), navigator.language.slice(0, 2), 'fr'].find(value => languages.includes(value));
let theme = [readPreference('spike-theme'), 'dark'].find(value => themes.includes(value));

function updatePreviews() {
  const copy = translations[language];
  document.querySelector('#overlay-preview').src = `assets/overlay-${language}-${theme}.png`;
  document.querySelector('#overlay-preview').alt = copy.overlayAlt;
  document.querySelector('#report-preview').src = `assets/report-${language}-${theme}.png`;
  document.querySelector('#report-preview').alt = copy.reportAlt;
  document.querySelector('#report-link').href = `assets/report-${language}-${theme}.png`;
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
