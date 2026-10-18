/* Own the complete gesture: a two-pointer interaction cannot end as a strike. */
(function(root){
 class PreviewInput {
  constructor(){this.points=new Map();this.multi=false;}
  down(e){this.points.set(e.id,{...e,startX:e.x,startY:e.y,startT:e.t});if(this.points.size>1)this.multi=true;return [];}
  move(e){
   const old=this.points.get(e.id);if(!old)return [];
   const before=[...this.points.values()];this.points.set(e.id,{...old,x:e.x,y:e.y});
   if(this.points.size===2){
    const after=[...this.points.values()];const dx=(after[0].x+after[1].x-before[0].x-before[1].x)/2;
    const distance=a=>Math.hypot(a[0].x-a[1].x,a[0].y-a[1].y);const a=distance(before),b=distance(after),out=[];
    if(dx)out.push({type:'orbit',dx});if(a>4&&b>4&&a!==b)out.push({type:'zoom',factor:a/b});return out;
   }
   if(!this.multi&&old.type==='mouse'&&old.button===2&&e.x!==old.x)return [{type:'orbit',dx:e.x-old.x}];
   return [];
  }
  up(e){
   const old=this.points.get(e.id),wasMulti=this.multi;this.points.delete(e.id);if(!this.points.size)this.multi=false;
   if(!old||wasMulti||(old.type==='mouse'&&old.button!==0))return [];
   const dx=e.x-old.startX,dy=e.y-old.startY;
   if(e.t-old.startT>900||Math.hypot(dx,dy)<45)return [];
   return [{type:'strike',kind:Math.abs(dx)>Math.abs(dy)?'punch':'kick'}];
  }
  cancel(id){this.points.delete(id);if(!this.points.size)this.multi=false;}
  reset(){this.points.clear();this.multi=false;}
 }
 if(typeof module!=='undefined'&&module.exports)module.exports=PreviewInput;else root.PreviewInput=PreviewInput;
})(typeof window!=='undefined'?window:this);
