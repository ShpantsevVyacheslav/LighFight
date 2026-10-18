const roster=[
 {id:'retro',name:'РЕТРО',style:'Темп и быстрые серии',label:'Ретро',power:1280,stats:[83,71,88]},
 {id:'sailor',name:'МОРЯК',style:'Стойкость и тяжёлые удары',label:'Моряк',power:1340,stats:[65,91,76]},
 {id:'summer',name:'ДАЧНИК',style:'Сила и ближняя дистанция',label:'Дачник',power:1310,stats:[72,86,80]}
];
const arenas=[{id:'meeting',name:'Переговорная'},{id:'yard',name:'Двор'},{id:'kitchen',name:'Кухня'}];
const defaults={sound:true,haptics:true,volume:70,controls:'Вместе',quality:'Максимум',fps:'60',hand:'Справа',scale:100,mode:'Против бота',difficulty:'Обычная',wins:'2',round:'60',arena:'meeting'};
let prefs={...defaults};try{prefs={...defaults,...JSON.parse(localStorage.getItem('lf.design.prefs')||'{}')}}catch{}
const $=s=>document.querySelector(s),dialog=$('#dialog'),viewer=$('#viewer');
let selected=0,opponent=null,screen='roster',loadedActor=null,readyActor=null,generation=0,previewMotion=null;
const portrait=x=>'../../art/portraits/generated-v1/'+x.id+'.png';
function save(){try{localStorage.setItem('lf.design.prefs',JSON.stringify(prefs))}catch{} paintSummary()}
function paintSummary(){
 $('#chosenName').textContent=roster[selected].label;$('#chosenStyle').textContent=roster[selected].style;
 $('#continue').textContent=screen==='roster'?'ПРОДОЛЖИТЬ':'В БОЙ';
 $('#ruleSummary').textContent=`${prefs.wins} победы · ${prefs.round} сек.`;
 const a=arenas.find(x=>x.id===prefs.arena)||arenas[0];$('#arenaPreviewName').textContent=a.name;$('#arenaPreviewImage').src='arenas/'+a.id+'.png';
 $('#opponentName').textContent=opponent===null?'Выбрать':roster[opponent].label;
}
function paint(){
 $('#fighters').innerHTML=roster.map((x,i)=>`<button class="fighter ${i===selected?'selected':''}" data-index="${i}" aria-pressed="${i===selected}" aria-label="Выбрать: ${x.label}"><img src="${portrait(x)}" alt="${x.label}"><span class="name">${x.name}</span><span class="style">${x.style}</span><span class="pick"></span></button>`).join('');
 document.querySelectorAll('.fighter').forEach(b=>b.onclick=()=>{selected=+b.dataset.index;openDetail()});paintSummary();
}
function toRoster(){screen='roster';$('.screen').classList.remove('detail-open');viewer.pause?.();$('#detail').hidden=true;$('.roster').hidden=false;paint()}
function openDetail(){
 screen='detail';$('.screen').classList.add('detail-open');$('.roster').hidden=true;$('#detail').hidden=false;
 if(opponent===selected)opponent=null;
 const x=roster[selected];$('#detailName').textContent=x.name;$('#detailStyle').textContent=x.style;$('#power').textContent=x.power.toLocaleString('ru');
 $('#attributes').innerHTML=['Скорость','Сила','Точность'].map((n,i)=>`<div><span>${n}</span><b>${x.stats[i]}</b><i><em style="width:${x.stats[i]}%"></em></i></div>`).join('');
 $('#actorCount').textContent=String(selected+1).padStart(2,'0')+' / 03';
 $('#dance').classList.remove('on');paintSummary();loadActor(x);
}
async function loadActor(actor){
 if(loadedActor===actor.id){if(readyActor===actor.id)stance();return}
 const ticket=++generation;loadedActor=actor.id;readyActor=null;previewMotion=null;viewer.pause?.();viewer.style.opacity=0;viewer.setAttribute('aria-busy','true');$('#modelStatus').textContent='Загрузка бойца…';$('#dance').disabled=true;
 try{
  const res=await fetch('models/'+actor.id+'.manifest.json');if(!res.ok)throw new Error('model manifest');const info=await res.json();if(ticket!==generation)return;viewer.src='models/'+actor.id+'.glb?v='+encodeURIComponent(info.glbSHA256||'native');
 }catch{if(ticket!==generation)return;loadedActor=null;viewer.setAttribute('aria-busy','false');$('#modelStatus').textContent='3D-просмотр пока недоступен';}
}
function actionName(kind){
 const names=viewer.availableAnimations||[];
 const patterns={guard:/PreviewGuard/i,dance:/dance/i,punch:/box_02|box_01|punch/i,kick:/front_kick|kick/i,greeting:/greet/i};return names.find(n=>patterns[kind].test(n));
}
function stance(){
 if(screen!=='detail'||!viewer.availableAnimations?.length)return;
 const idle=actionName('guard');previewMotion=null;$('#dance').classList.remove('on');
 if(idle){viewer.animationName=idle;viewer.play();}
 else {const boxing=actionName('punch');if(boxing){viewer.animationName=boxing;viewer.currentTime=0;viewer.pause();}}
}
function playMotion(kind){
 const name=actionName(kind);if(!name||screen!=='detail')return;
 // A new request cannot truncate an authored attack; wait for its finished event.
 if(previewMotion&&previewMotion!=='dance')return;
 if(kind==='dance'&&previewMotion==='dance'){stance();return}
 previewMotion=kind;viewer.animationName=name;viewer.currentTime=0;viewer.play({repetitions:1});$('#dance').classList.toggle('on',kind==='dance');
}
viewer.addEventListener('load',()=>{readyActor=loadedActor;viewer.style.opacity=1;viewer.setAttribute('aria-busy','false');$('#modelStatus').textContent='';$('#dance').disabled=!actionName('dance');stance()});
viewer.addEventListener('error',()=>{readyActor=null;viewer.style.opacity=0;viewer.setAttribute('aria-busy','false');$('#modelStatus').textContent='Не удалось загрузить 3D-модель';loadedActor=null});
viewer.addEventListener('finished',()=>{if(screen==='detail')stance()});
$('#dance').onclick=()=>playMotion('dance');
const pointers=new Map();let swipe=null;
viewer.addEventListener('pointerdown',e=>{pointers.set(e.pointerId,true);if(pointers.size>1){swipe=null;return}swipe={id:e.pointerId,x:e.clientX,y:e.clientY,t:performance.now()}});
viewer.addEventListener('pointerup',e=>{pointers.delete(e.pointerId);if(!swipe||swipe.id!==e.pointerId)return;const s=swipe;swipe=null;const dx=e.clientX-s.x,dy=e.clientY-s.y;if(performance.now()-s.t<350&&Math.hypot(dx,dy)>50)playMotion(Math.abs(dx)>Math.abs(dy)?'punch':'kick')});
viewer.addEventListener('pointercancel',e=>{pointers.delete(e.pointerId);swipe=null});
$('#previous').onclick=()=>{selected=(selected+roster.length-1)%roster.length;openDetail()};$('#next').onclick=()=>{selected=(selected+1)%roster.length;openDetail()};
function show(html,kind){$('#dialogContent').innerHTML=html;dialog.dataset.kind=kind||'';dialog.showModal()}
function choices(key,values){return `<div class="options" data-key="${key}">${values.map(v=>`<button class="${prefs[key]===String(v)?'active':''}" data-value="${v}" aria-pressed="${prefs[key]===String(v)}">${v}</button>`).join('')}</div>`}
function wireChoices(){dialog.querySelectorAll('[data-key] button').forEach(b=>b.onclick=()=>{const group=b.closest('[data-key]');prefs[group.dataset.key]=b.dataset.value;group.querySelectorAll('button').forEach(x=>{x.classList.toggle('active',x===b);x.setAttribute('aria-pressed',String(x===b))});save()})}
function rules(){show(`<span class="eyebrow">НАСТРОЙ БОЙ</span><h2>По твоим правилам</h2><div class="rule-grid"><section><label>Режим</label>${choices('mode',['Против бота','Тренировка'])}<label>Сложность</label>${choices('difficulty',['Лёгкая','Обычная','Сложная'])}</section><section><label>До побед</label>${choices('wins',['1','2','3'])}<label>Раунд, сек.</label>${choices('round',['60','90','120'])}</section></div><button class="continue apply">ГОТОВО <span>✓</span></button>`,'rules');wireChoices();dialog.querySelector('.apply').onclick=()=>dialog.close()}
function settings(){
 show(`<span class="eyebrow">LIGHFIGHT</span><h2>Настройки игры</h2><div class="settings-tabs"><button class="active" data-tab="controls">Управление</button><button data-tab="graphics">Графика</button><button data-tab="audio">Звук</button></div><div id="settingsBody"></div>`,'settings');
 const panel=kind=>{
  const node=$('#settingsBody');
  if(kind==='controls')node.innerHTML=`<label>Удары</label>${choices('controls',['Свайпы','Кнопки','Вместе'])}<label>Кнопки ударов</label>${choices('hand',['Слева','Справа'])}<div class="setting-row"><span>Размер кнопок</span><input id="scale" type="range" min="80" max="130" value="${prefs.scale}" aria-label="Размер кнопок"></div>`;
  if(kind==='graphics')node.innerHTML=`<label>Качество</label>${choices('quality',['Экономия','Высокое','Максимум'])}<label>Частота кадров</label>${choices('fps',['30','60'])}`;
  if(kind==='audio')node.innerHTML=`<div class="setting-row"><span>Звук</span><input id="sound" type="checkbox" ${prefs.sound?'checked':''} aria-label="Звук"></div><div class="setting-row"><span>Громкость</span><input id="volume" type="range" min="0" max="100" value="${prefs.volume}" aria-label="Громкость"></div><div class="setting-row"><span>Вибрация</span><input id="haptics" type="checkbox" ${prefs.haptics?'checked':''} aria-label="Вибрация"></div>`;
  wireChoices();node.querySelectorAll('input').forEach(input=>input.oninput=()=>{prefs[input.id]=input.type==='checkbox'?input.checked:Number(input.value);save()});
 };
 dialog.querySelectorAll('[data-tab]').forEach(b=>b.onclick=()=>{dialog.querySelectorAll('[data-tab]').forEach(x=>x.classList.toggle('active',x===b));panel(b.dataset.tab)});panel('controls');
}
function arena(){show(`<span class="eyebrow">МЕСТО ВСТРЕЧИ</span><h2>Выбери арену</h2><div class="arena-grid">${arenas.map(a=>`<button class="arena-option ${prefs.arena===a.id?'selected':''}" data-arena="${a.id}"><img src="arenas/${a.id}.png" alt=""><span>${a.name}</span><small>${prefs.arena===a.id?'ВЫБРАНА':'ВЫБРАТЬ'}</small></button>`).join('')}</div>`,'arena');dialog.querySelectorAll('[data-arena]').forEach(b=>b.onclick=()=>{prefs.arena=b.dataset.arena;save();dialog.close()})}
function opponents(){
 if(opponent===null)opponent=(selected+1)%roster.length;
 const content=()=>`<span class="eyebrow">ПЕРЕД БОЕМ</span><h2>Выбери соперника</h2><div class="opponent-grid">${roster.map((x,i)=>i===selected?'':`<button class="opponent-option ${i===opponent?'selected':''}" data-opponent="${i}" aria-pressed="${i===opponent}"><img src="${portrait(x)}" alt="${x.label}"><strong>${x.label}</strong><small>${x.style}</small></button>`).join('')}</div><button class="continue apply">ПОДТВЕРДИТЬ <span>✓</span></button>`;
 show(content(),'opponent');const wire=()=>{dialog.querySelectorAll('[data-opponent]').forEach(b=>b.onclick=()=>{opponent=+b.dataset.opponent;$('#dialogContent').innerHTML=content();wire()});dialog.querySelector('.apply').onclick=()=>{paintSummary();dialog.close()}};wire();
}
$('#settings').onclick=settings;$('#rules').onclick=rules;$('#arenaTab').onclick=$('#detailArena').onclick=arena;$('#opponent').onclick=opponents;
$('#continue').onclick=()=>screen==='roster'?openDetail():opponents();$('#back').onclick=()=>{if(dialog.open)dialog.close();else toRoster()};$('#rosterTab').onclick=()=>{if(dialog.open)dialog.close();toRoster()};
dialog.addEventListener('close',()=>{if(screen==='detail'&&!previewMotion)stance()});paint();
