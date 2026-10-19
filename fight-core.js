/* Fixed-step combat simulation. Values are metres, seconds and damage points. */
(function(root){
const MOVES={
 jab:{label:'Джеб',startup:.10,active:.075,recovery:.20,damage:7,range:1.25,level:'high',frames:[4,5],push:.08,stamina:5,hitstun:.24,blockstun:.08,guardDamage:7},
 cross:{label:'Кросс',startup:.17,active:.085,recovery:.28,damage:11,range:1.42,level:'high',frames:[4,5],push:.15,stamina:8,hitstun:.30,blockstun:.11,guardDamage:10},
 hook:{label:'Хук',startup:.21,active:.09,recovery:.31,damage:14,range:1.08,level:'high',frames:[4,12],push:.17,stamina:10,hitstun:.34,blockstun:.12,guardDamage:13},
 body:{label:'Удар в корпус',startup:.15,active:.075,recovery:.25,damage:9,range:1.18,level:'mid',frames:[8,5],push:.09,stamina:7,hitstun:.26,blockstun:.09,guardDamage:9},
 kick:{label:'Боковой удар',startup:.24,active:.11,recovery:.34,damage:16,range:1.98,level:'mid',frames:[6,7],push:.34,stamina:13,hitstun:.34,blockstun:.16,guardDamage:17},
 lowkick:{label:'Лоу-кик',startup:.18,active:.09,recovery:.29,damage:10,range:1.65,level:'low',frames:[6,14],push:.10,stamina:8,hitstun:.28,blockstun:.10,guardDamage:10},
 sweep:{label:'Подсечка',startup:.30,active:.12,recovery:.46,damage:14,range:1.72,level:'low',frames:[11,14],push:.25,stamina:16,hitstun:.36,blockstun:.19,guardDamage:15,knockdown:true},
 uppercut:{label:'Апперкот',startup:.25,active:.09,recovery:.43,damage:18,range:1.20,level:'mid',frames:[8,9],push:.15,stamina:16,hitstun:.38,blockstun:.17,guardDamage:18,launch:true},
 knee:{label:'Колено',startup:.19,active:.09,recovery:.29,damage:13,range:.98,level:'mid',frames:[6,15],push:.13,stamina:10,hitstun:.31,blockstun:.11,guardDamage:12},
 elbow:{label:'Локоть',startup:.14,active:.065,recovery:.27,damage:11,range:.95,level:'mid',frames:[4,12],push:.12,stamina:8,hitstun:.27,blockstun:.09,guardDamage:10},
 spinning:{label:'Удар с разворота',startup:.38,active:.13,recovery:.48,damage:24,range:2.08,level:'high',frames:[6,13],push:.52,stamina:22,hitstun:.46,blockstun:.19,guardDamage:25,knockdown:true},
 jumpkick:{label:'Удар в прыжке',startup:.30,active:.12,recovery:.43,damage:19,range:1.90,level:'mid',frames:[6,15],push:.35,stamina:18,hitstun:.37,blockstun:.16,guardDamage:20,hop:true},
 throw:{label:'Захват',startup:.18,active:.08,recovery:.48,damage:20,range:.96,level:'throw',frames:[4,12],push:.45,stamina:15,hitstun:.5,blockstun:0,guardDamage:0,knockdown:true},
 special:{label:'Силовой приём',startup:.42,active:.15,recovery:.65,damage:30,range:2.12,level:'mid',frames:[8,9],push:.55,stamina:24,hitstun:.50,blockstun:.22,guardDamage:30,cost:60,launch:true}
};
const ROUTES={jab:['cross','body'],cross:['hook','kick','uppercut'],body:['knee','elbow'],hook:['kick'],lowkick:['cross'],knee:['uppercut'],elbow:['hook'],uppercut:['jab','kick'],kick:['spinning']};
const DEFAULT={speed:3.25,power:1,reach:1,maxHP:160,maxStamina:100};
const clamp=(v,min,max)=>Math.max(min,Math.min(max,v));
class Fight{
 constructor(){this.profiles=[{},{}];this.reset(false)}
 configure(profiles){this.profiles=profiles}
 reset(keep){const old=this.fighters;this.fighters=[0,1].map(i=>{const stats={...DEFAULT,...this.profiles[i]};return{x:i?2.8:-2.8,y:0,vy:0,vx:0,hp:stats.maxHP,stats,stamina:stats.maxStamina,energy:0,face:i?-1:1,wins:keep?old[i].wins:0,move:null,stun:0,guard:false,crouch:false,dodge:0,dodgeAge:0,dodgeCooldown:0,down:0,combo:0,comboTimer:0,airHits:0,walk:0,buffer:null,jumpHeld:false,dash:0,dashDir:0,grabbed:null}});this.remaining=60;this.status='playing';this.events=[];this.hitstop=0;this.round=keep?(this.round||1)+1:1}
 command(i,type,direction){const f=this.fighters[i];if(this.status!=='playing')return;
 if(type==='throw'&&f.grabbed){const a=this.fighters[f.grabbed.attacker];a.stun=.18;a.move=null;f.grabbed=null;f.stun=0;this.pendingEvents??=[];this.pendingEvents.push({type:'escape',x:f.x,y:f.y,attacker:i});return}
 if(type==='dodge'){if(!f.move&&!f.stun&&!f.down&&!f.grabbed&&f.y===0&&f.dodgeCooldown<=0&&f.stamina>=12){f.dodge=.30;f.dodgeAge=0;f.dodgeCooldown=.8;f.stamina-=12;f.guard=false;f.buffer=null}return}
 if(type==='dash'){if(!f.move&&!f.stun&&!f.down&&!f.grabbed&&f.y===0&&f.stamina>=6){f.dash=.17;f.dashDir=direction===-1?-1:1;f.stamina-=6}return}
 if(!MOVES[type]||f.grabbed||f.down)return;f.buffer={type,ttl:.22}
 }
 startMove(f,type){const m=MOVES[type];f.buffer=null;if(f.energy<(m.cost||0)||f.stamina<m.stamina)return false;f.energy-=m.cost||0;f.stamina-=m.stamina;f.move={type,t:0,connected:false,face:f.face};f.guard=false;f.vx*=.2;if(m.hop&&f.y===0)f.vy=3.8;return true}
 step(dt,inputs=[{},{}]){this.events=this.pendingEvents||[];this.pendingEvents=[];if(this.status!=='playing')return;this.remaining=Math.max(0,this.remaining-dt);if(this.hitstop>0){this.hitstop=Math.max(0,this.hitstop-dt);this.finish();return}
 // Update both fighters before evaluating contacts: same-frame attacks can trade.
 this.fighters.forEach((f,i)=>{const input=inputs[i]||{},enemy=this.fighters[1-i];if(!f.move&&!f.grabbed)f.face=enemy.x>=f.x?1:-1;f.stun=Math.max(0,f.stun-dt);f.down=Math.max(0,f.down-dt);f.dodge=Math.max(0,f.dodge-dt);if(f.dodge)f.dodgeAge+=dt;f.dodgeCooldown=Math.max(0,f.dodgeCooldown-dt);f.dash=Math.max(0,f.dash-dt);f.comboTimer=Math.max(0,f.comboTimer-dt);if(!f.comboTimer)f.combo=0;f.stamina=Math.min(f.stats.maxStamina,f.stamina+dt*(f.move?4:f.guard?6:18));f.energy=Math.min(100,f.energy+dt*.7);
 f.crouch=!!input.crouch&&f.y===0&&!f.move&&!f.down;f.guard=!!input.guard&&!f.move&&!f.stun&&!f.dodge&&!f.down&&!f.grabbed&&f.y===0;
 if(f.buffer){f.buffer.ttl-=dt;if(f.buffer.ttl<=0)f.buffer=null}
 if(f.grabbed){f.grabbed.t-=dt;if(f.grabbed.t<=0)this.resolveThrow(i);return}
 if(!f.move&&!f.stun&&!f.dodge&&!f.down){if(f.buffer&&!f.guard)this.startMove(f,f.buffer.type);const dir=(input.right?1:0)-(input.left?1:0),target=f.move||f.guard?0:dir*f.stats.speed*(f.crouch?.4:1);const acceleration=target?24:30;f.vx+=clamp(target-f.vx,-acceleration*dt,acceleration*dt);if(input.jump&&!f.jumpHeld&&!f.move&&!f.guard&&f.y===0)f.vy=5.9;f.walk+=Math.abs(f.vx)*dt*3}else f.vx*=Math.max(0,1-dt*20);
 f.jumpHeld=!!input.jump;if(f.dash&&!f.move&&!f.stun&&!f.down)f.vx=f.dashDir*6;f.x=clamp(f.x+f.vx*dt,-5.3,5.3);f.y+=f.vy*dt;f.vy-=18*dt;if(f.y<=0){const wasAir=f.y<0&&f.vy<-6;f.y=0;f.vy=0;if(wasAir&&f.airHits){f.down=.65;f.stun=Math.max(f.stun,.18)}f.airHits=0}
 if(f.move)f.move.t+=dt;
 });
 const contacts=[];
 this.fighters.forEach((f,i)=>{if(!f.move)return;const a=f.move,m=MOVES[a.type],enemy=this.fighters[1-i];if(a.t<m.startup||a.t>m.startup+m.active||a.connected)return;const distance=(enemy.x-f.x)*a.face,range=m.range*f.stats.reach;const dodge=enemy.dodge>0&&enemy.dodgeAge>=.045&&enemy.dodgeAge<=.22;const duck=m.level==='high'&&enemy.crouch;const grounded=enemy.down>.35||enemy.down>0&&m.level!=='low';const height=m.level==='low'?Math.abs(f.y-enemy.y)<.6:Math.abs(f.y-enemy.y)<1.25;
 if(distance<0||distance>range||!height||dodge||duck||grounded||enemy.grabbed||enemy.airHits>=3)return;a.connected=true;
 if(m.level==='throw'){if(enemy.y>0||enemy.move?.type==='throw')return;enemy.grabbed={attacker:i,t:.28};enemy.guard=false;enemy.move=null;enemy.buffer=null;f.move.t=m.startup+m.active;this.events.push({type:'grab',x:enemy.x,y:0,attacker:i});return}
 const front=(f.x-enemy.x)*enemy.face>=0;const blocked=enemy.guard&&front&&(m.level==='low'?enemy.crouch:!enemy.crouch);contacts.push({i,m,type:a.type,blocked,counter:!!enemy.move&&enemy.move.t<MOVES[enemy.move.type].startup,punish:!!enemy.move&&enemy.move.t>MOVES[enemy.move.type].startup+MOVES[enemy.move.type].active})
 });
 contacts.forEach(c=>this.contact(c));
 this.fighters.forEach(f=>{if(!f.move)return;const a=f.move,m=MOVES[a.type];if(a.t>=m.startup+m.active+m.recovery)f.move=null;else if(a.connected&&f.buffer&&f.combo<4&&a.t>=m.startup+m.active+.065&&ROUTES[a.type]?.includes(f.buffer.type)){const next=f.buffer.type;f.move=null;this.startMove(f,next)}});
 const [a,b]=this.fighters;if(Math.abs(a.x-b.x)<.8&&Math.abs(a.y-b.y)<1&&!a.grabbed&&!b.grabbed){const mid=(a.x+b.x)/2,d=a.x<=b.x?1:-1;const low=clamp(mid-.4,-5.3,4.5);a.x=d>0?low:low+.8;b.x=d>0?low+.8:low}this.finish()
 }
 contact(c){const f=this.fighters[c.i],enemy=this.fighters[1-c.i],m=c.m;let type=c.blocked?'block':'hit',damage=0;if(c.blocked){enemy.stamina=Math.max(0,enemy.stamina-m.guardDamage);enemy.stun=m.blockstun;if(enemy.stamina===0){enemy.guard=false;enemy.stun=.7;type='guardBreak'}enemy.x=clamp(enemy.x+f.face*m.push*.45,-5.3,5.3)}else{damage=Math.ceil(m.damage*f.stats.power*Math.max(.4,1-f.combo*.13)*(c.counter?1.2:c.punish?1.1:1));enemy.hp=Math.max(0,enemy.hp-damage);enemy.stun=m.hitstun+(c.counter?.08:0);enemy.move=null;enemy.buffer=null;enemy.dash=0;enemy.x=clamp(enemy.x+f.face*m.push,-5.3,5.3);enemy.vx=f.face*.6;f.combo++;f.comboTimer=1.1;f.energy=Math.min(100,f.energy+9);enemy.energy=Math.min(100,enemy.energy+5);if(enemy.y>0){enemy.airHits++;enemy.vy=Math.min(3.1,Math.max(enemy.vy,1.8))}else if(m.launch){enemy.vy=6.4;enemy.y=.02;enemy.airHits=1}else if(m.knockdown)enemy.down=.85}
 this.hitstop=Math.max(this.hitstop,c.blocked?.028:.055);this.events.push({type,x:enemy.x,y:enemy.y,damage,combo:f.combo,attacker:c.i,special:c.type==='special',counter:c.counter&&!c.blocked,punish:c.punish&&!c.blocked,move:c.type})
 }
 resolveThrow(i){const enemy=this.fighters[i],a=this.fighters[enemy.grabbed.attacker];const attacker=enemy.grabbed.attacker;enemy.grabbed=null;const damage=Math.ceil(MOVES.throw.damage*a.stats.power);enemy.hp=Math.max(0,enemy.hp-damage);enemy.x=clamp(a.x+a.face*1.1,-5.3,5.3);enemy.down=1;enemy.stun=.3;a.energy=Math.min(100,a.energy+12);this.hitstop=.08;this.events.push({type:'hit',x:enemy.x,y:0,damage,combo:1,attacker,move:'throw'})}
 finish(){const [a,b]=this.fighters;if(a.hp<=0||b.hp<=0||this.remaining<=0){this.winner=a.hp===b.hp?null:a.hp>b.hp?0:1;if(this.winner!==null)this.fighters[this.winner].wins++;this.status=this.winner!==null&&this.fighters[this.winner].wins>=2?'matchEnd':'roundEnd';this.events.push({type:'finish',winner:this.winner})}}
 pose(f){if(f.down)return 10;if(f.grabbed||f.stun)return 10;if(f.dodge)return 11;if(f.move){const m=MOVES[f.move.type];return f.move.t<m.startup?m.frames[0]:f.move.t<m.startup+m.active?m.frames[1]:m.frames[0]}if(f.guard)return f.crouch?11:3;if(f.crouch)return 11;return f.vx?Math.floor(f.walk)%3:Math.floor(f.walk*.2)%3}
}
root.Fight=Fight;root.FIGHT_MOVES=MOVES;if(typeof module!=='undefined')module.exports={Fight,MOVES,ROUTES};
})(typeof window==='undefined'?globalThis:window);
