// For a page shown inside an in-game screen page as its main: or banner: frame: the same view
// of what the screen plays that the screen page itself gets from the launcher hook. Include it
// (<script src="/ai-sp/aisp-frame.js"></script>, or the absolute URL on the emulator's host)
// and use any of:
//   window.aisp            the latest media state: source, kind, url, title, duration,
//                          position, elapsed, playing, paused, active, status, fps, box, crop
//                          (null until the screen has something to say)
//   window.onAisp(media)   called on every update, like in the screen page
//   document 'aisp' event  detail = { media, screen }
//   window.aispFrame       .media, .screen ({ route, kind, params, src, main, banner }) and
//                          .on(fn), which also calls fn at once if state is already known
// The screen page relays its state with postMessage on every change, when this frame loads,
// and once more on request; a page loaded on its own (not in a frame) just never hears any.
(function(){
  var state={ media:null, screen:null }, listeners=[];
  function tell(){
    for(var i=0;i<listeners.length;i++){ try{ listeners[i](state); }catch(e){} }
    if(typeof window.onAisp==='function'){ try{ window.onAisp(state.media); }catch(e){} }
    try{ document.dispatchEvent(new CustomEvent('aisp',{detail:state})); }catch(e){}
  }
  window.aispFrame={
    get media(){ return state.media; },
    get screen(){ return state.screen; },
    on:function(fn){ listeners.push(fn); if(state.screen){ try{ fn(state); }catch(e){} } return fn; }
  };
  window.addEventListener('message',function(e){
    var d=e.data;
    if(!d||d.type!=='aisp') return;
    state.media=d.media||null; state.screen=d.screen||null;
    window.aisp=state.media;
    tell();
  });
  try{ if(window.parent&&window.parent!==window) window.parent.postMessage({type:'aisp:get'},'*'); }catch(e){}
})();
