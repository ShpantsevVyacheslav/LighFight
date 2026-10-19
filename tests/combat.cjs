const assert=require('node:assert/strict');
const {Fight,MOVES}=require('../fight-core.js');
const tests=[];
function test(name,fn){fn();tests.push(name)}
function near(distance=1){const g=new Fight();g.fighters[0].x=0;g.fighters[1].x=distance;return g}
function tick(g,n,inputs){for(let i=0;i<n;i++)g.step(1/120,inputs)}
function damage(g){return g.fighters[1].stats.maxHP-g.fighters[1].hp}

test('14 distinct attacks',()=>assert.equal(Object.keys(MOVES).length,14));
test('startup and one contact per attack',()=>{const g=near();g.command(0,'jab');tick(g,8);assert.equal(damage(g),0);tick(g,12);assert.equal(damage(g),7);tick(g,50);assert.equal(damage(g),7)});
test('distance and orientation',()=>{const g=near(3);g.command(0,'jab');tick(g,60);assert.equal(damage(g),0);const n=near();n.fighters[0].move={type:'jab',t:.1,face:-1,connected:false};tick(n,1);assert.equal(damage(n),0)});
test('high guard absorbs health damage and consumes stamina',()=>{const g=near();g.command(0,'jab');tick(g,25,[{},{guard:true}]);assert.equal(damage(g),0);assert(g.fighters[1].stamina<100)});
test('crouching evades high attacks',()=>{const g=near();g.command(0,'jab');tick(g,30,[{},{crouch:true}]);assert.equal(damage(g),0)});
test('mid attack defeats crouch guard',()=>{const g=near();g.command(0,'body');tick(g,40,[{},{guard:true,crouch:true}]);assert.equal(damage(g),9)});
test('low attack defeats standing guard',()=>{const g=near();g.command(0,'lowkick');tick(g,40,[{},{guard:true}]);assert.equal(damage(g),10)});
test('crouch guard stops low attack',()=>{const g=near();g.command(0,'sweep');tick(g,60,[{},{guard:true,crouch:true}]);assert.equal(damage(g),0)});
test('exhaustion breaks guard',()=>{const g=near();g.fighters[1].stamina=1;g.command(0,'cross');tick(g,32,[{},{guard:true}]);assert.equal(damage(g),0);assert(g.fighters[1].stun>.4);assert.equal(g.fighters[1].guard,false)});
test('finite dodge window',()=>{const g=near();g.command(1,'dodge');g.command(0,'jab');tick(g,30);assert.equal(damage(g),0);const n=near();n.command(1,'dodge');n.command(0,'kick');tick(n,50);assert.equal(damage(n),16)});
test('stamina and energy gate moves',()=>{const g=near();g.fighters[0].stamina=0;g.command(0,'kick');tick(g,50);assert.equal(damage(g),0);g.command(0,'special');tick(g,80);assert.equal(damage(g),0);g.fighters[0].energy=100;g.fighters[0].stamina=100;g.command(0,'special');tick(g,80);assert.equal(damage(g),30);assert(g.fighters[0].energy<60)});
test('counter hit and recovery punish',()=>{const g=near();g.command(1,'special');g.fighters[1].energy=100;g.command(0,'jab');tick(g,25);assert.equal(damage(g),9);const n=near();n.fighters[1].move={type:'kick',t:.36,face:-1,connected:true};n.command(0,'jab');tick(n,25);assert.equal(damage(n),8)});
test('same-frame strikes trade symmetrically',()=>{const g=near();g.command(0,'jab');g.command(1,'jab');tick(g,22);assert.equal(g.fighters[0].hp,g.fighters[1].hp);assert.equal(damage(g),7)});
test('legal route and combo scaling',()=>{const g=near();g.command(0,'jab');tick(g,26);g.command(0,'cross');tick(g,37);assert.equal(g.fighters[0].combo,2);assert.equal(damage(g),17)});
test('illegal cancels wait for recovery',()=>{const g=near();g.command(0,'spinning');tick(g,55);g.command(0,'jab');tick(g,5);assert.equal(g.fighters[0].move.type,'spinning')});
test('launch and automatic get-up',()=>{const g=near();g.command(0,'uppercut');tick(g,48);assert(g.fighters[1].y>0);tick(g,180);assert.equal(g.fighters[1].y,0);assert.equal(g.fighters[1].down,0)});
test('throw bypasses guard with break window',()=>{const g=near(.85);g.command(0,'throw');tick(g,26,[{},{guard:true}]);assert(g.fighters[1].grabbed);g.command(1,'throw');tick(g,60);assert.equal(damage(g),0);assert.equal(g.fighters[1].grabbed,null);const n=near(.85);n.command(0,'throw');tick(n,70,[{},{guard:true}]);assert.equal(damage(n),20);assert(n.fighters[1].down>0)});
test('held jump cannot repeat automatically',()=>{const g=near();tick(g,200,[{jump:true},{}]);assert.equal(g.fighters[0].y,0);tick(g,1,[{},{}]);tick(g,10,[{jump:true},{}]);assert(g.fighters[0].y>0)});
test('acceleration, dash and wall separation',()=>{const g=near();g.step(1/120,[{left:true},{}]);assert(g.fighters[0].vx<0&&g.fighters[0].vx>-3.25);g.command(0,'dash',-1);tick(g,12);assert(g.fighters[0].x<-.4);g.fighters[0].x=g.fighters[1].x=5.3;tick(g,1);assert(Math.abs(g.fighters[0].x-g.fighters[1].x)>=.799)});
test('profiles, round wins and reset',()=>{const g=near();g.configure([{maxHP:170,power:1.1},{}]);g.reset(false);assert.equal(g.fighters[0].hp,170);g.fighters[1].hp=0;tick(g,1);assert.equal(g.status,'roundEnd');g.reset(true);assert.equal(g.fighters[0].wins,1);g.fighters[1].hp=0;tick(g,1);assert.equal(g.status,'matchEnd');g.reset(false);assert.equal(g.fighters[0].wins,0)});
test('timeout draw',()=>{const g=near();g.remaining=.001;tick(g,1);assert.equal(g.status,'roundEnd');assert.equal(g.winner,null)});
console.log(`${tests.length} combat checks passed.`);
