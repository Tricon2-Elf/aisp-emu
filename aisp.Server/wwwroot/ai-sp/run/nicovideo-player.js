// Run by the launcher hook's off-screen browser in a Nico video watch page
// (https://www.nicovideo.jp/watch/sm…#start=<unix>&offset=<s>[&paused=<unix>]), the nne:sm…
// source. The page's own player stays in charge (moving its elements out from under it left
// its state, and so the comment renderer, stuck): this presses its 全画面表示 button, which
// puts the player over the viewport (the video box), hides every sibling on the way from the
// video up to that fullscreen box that holds neither the video nor a comment canvas (the top
// banner, the controls, the comment form), and then steers the video element: volume up, the
// shared timeline's position (the fragment, ignored by the site; modulo the length, within
// kEdgeSeconds of either end from 0, the hook's ffmpeg rule) once the player itself has
// started it, loop so it never reaches the end screen or the next video, pause for a paused
// timeline. A pre-roll ad runs first: where it is a video of the page's own it is muted, hidden
// and wound to its end at once (its skip button pressed as a fallback), and the player starts
// the feature the moment it is done. The comments come in the site's language, which follows
// the browser's Accept-Language unless the lang cookie says otherwise: without lang=ja-jp the
// page loads the sparse en-us thread and no easy comments, so the cookie is set once (the
// browser host's profile keeps it) and the page reloaded that one time. The title loses the
// site's suffix. The page renders late, so this keeps checking for a while.
(function(){
  if(window.__aispNicoVideo) return;
  window.__aispNicoVideo=true;
  if(!/(^|;\s*)lang=ja-jp(;|$)/.test(document.cookie||'')){
    document.cookie='lang=ja-jp; domain=.nicovideo.jp; path=/; max-age=31536000; secure; samesite=none';
    if(/(^|;\s*)lang=ja-jp(;|$)/.test(document.cookie||'')){ location.reload(); return; }
  }
  var kEdgeSeconds=3;
  var q={}, hash=(location.hash||'').replace(/^#/,'').split('&');
  for(var i=0;i<hash.length;i++){ var kv=hash[i].split('='); if(kv[0]) q[decodeURIComponent(kv[0])]=decodeURIComponent(kv[1]||''); }
  var start=+q.start||0, offset=+q.offset||0, paused=+q.paused||0;
  function target(){ var until=paused?paused:Math.floor(new Date().getTime()/1000); return offset+Math.max(0,until-start); }
  function startPosition(d){ if(!start||!(d>0)) return 0; var pos=target()%d; return (pos<kEdgeSeconds||pos>d-kEdgeSeconds)?0:pos; }
  var style=document.createElement('style');
  style.textContent=[
    'html,body{overflow:hidden!important;background:#000!important}',
    '.__aisp-hide{display:none!important}',
    // Ads: their video, and whatever is laid over the picture in a frame.
    'video.__aisp-ad,:fullscreen iframe{display:none!important}'
  ].join('');
  (document.head||document.documentElement).appendChild(style);
  var main=null, seeked=false, seekedFor=0, tries=0, wrapped=0;
  var wound=[]; // ad elements already wound to their end
  var ownUrl=location.href, ownPath=location.pathname;
  window.aispPlayer={mode:null, position:null, seeks:0, fullscreen:false, hidden:0, skips:0, playerStarted:false};
  function buttonWith(re){ var all=[].slice.call(document.querySelectorAll('button')); for(var i=0;i<all.length;i++){ if(re.test(all[i].getAttribute('aria-label')||'')) return all[i]; } return null; }
  function pick(){
    var vids=[].slice.call(document.querySelectorAll('video')).filter(function(v){ return v.currentSrc&&v.duration>60; });
    vids.sort(function(a,b){ return (b.duration||0)-(a.duration||0); });
    return vids[0]||null;
  }
  // The chrome: on the way from the video up to the fullscreen box, every sibling that holds
  // neither the video nor a canvas goes; the rest (the comment layers) stays where it is.
  function hideChrome(fs){
    var n=0, e=main;
    while(e&&e!==fs&&e.parentElement){
      var kids=[].slice.call(e.parentElement.children);
      for(var i=0;i<kids.length;i++){
        var k=kids[i];
        if(k===e||k.contains(main)||k.querySelector('canvas')||k.tagName==='CANVAS') continue;
        if(!k.classList.contains('__aisp-hide')){ k.classList.add('__aisp-hide'); n++; }
      }
      e=e.parentElement;
    }
    // The "貢献しませんか" bar over the picture is not a sibling on that chain: found by its
    // text, hidden with the largest ancestor that holds neither the video nor a canvas.
    var links=[].slice.call(document.querySelectorAll('a,span')).filter(function(a){ return a.children.length<=1&&/貢献しませんか/.test(a.textContent||''); });
    for(var j=0;j<links.length;j++){
      var t=links[j], top=t;
      while(top.parentElement&&top.parentElement!==fs&&!top.parentElement.contains(main)&&!top.parentElement.querySelector('canvas')) top=top.parentElement;
      if(!top.classList.contains('__aisp-hide')){ top.classList.add('__aisp-hide'); n++; }
    }
    window.aispPlayer.hidden+=n;
  }
  // The player's own continuous play moves on at the end (loop only spares the element's
  // ended event, not the player's clock, which does not follow an element seek backwards
  // while playing): just short of the end, press the player's own "0" key, a seek to the start
  // that its clock and comments follow. On the element's events, since timers on this page run
  // late; the shared timeline never lands in the last three seconds anyway.
  function wrapToStart(){
    if(Date.now()-wrapped<5000) return;
    wrapped=Date.now();
    try{
      var opts={key:'0',code:'Digit0',keyCode:48,which:48,bubbles:true,cancelable:true};
      document.body.dispatchEvent(new KeyboardEvent('keydown',opts));
      document.body.dispatchEvent(new KeyboardEvent('keyup',opts));
      window.aispPlayer.wraps=(window.aispPlayer.wraps||0)+1;
    }catch(e){}
  }
  function watchEnd(v){
    if(v.__aispWatched) return;
    v.__aispWatched=true;
    v.addEventListener('timeupdate',function(){ if(v===main&&v.duration>60&&v.currentTime>v.duration-1.5) wrapToStart(); });
    v.addEventListener('ended',function(){ if(v===main&&v.duration>60){ wrapToStart(); try{ var p=v.play(); if(p&&p.catch) p.catch(function(){}); }catch(e){} } });
  }
  function skipAd(){
    var all=[].slice.call(document.querySelectorAll('button'));
    for(var i=0;i<all.length;i++){ var l=all[i].textContent||all[i].getAttribute('aria-label')||''; if(/スキップ/.test(l)&&!/秒後/.test(l)){ try{ all[i].click(); window.aispPlayer.skips++; }catch(e){} return; } }
  }
  function step(){
    tries++;
    var v=pick();
    if(v&&v!==main){ main=v; seeked=false; watchEnd(main); }
    // Every other video is an ad: muted, hidden once its short length is known, and wound to
    // its last moment so the player takes it as watched.
    var adPlaying=false;
    [].slice.call(document.querySelectorAll('video')).forEach(function(o){
      if(o===main){ o.classList.remove('__aisp-ad'); return; }
      try{ o.muted=true; }catch(e){}
      if(o.currentSrc&&o.duration>0&&o.duration<=60){
        o.classList.add('__aisp-ad');
        // Once loaded: started if the player left it waiting (the feature waits on it), and
        // wound to its last moment, once; a re-seek of a stalled ad only stalls it more.
        if(o.readyState>=2){
          if(o.paused&&!o.ended){ try{ var ap=o.play(); if(ap&&ap.catch) ap.catch(function(){}); }catch(e){} }
          if(wound.indexOf(o)<0&&!o.ended&&o.duration-o.currentTime>0.5){ wound.push(o); try{ o.currentTime=o.duration-0.1; window.aispPlayer.adsWound=(window.aispPlayer.adsWound||0)+1; }catch(e){} }
        }
        if(!o.paused&&!o.ended) adPlaying=true;
      }
    });
    skipAd();
    if(!document.fullscreenElement){ var fb=buttonWith(/^全画面表示する$/); if(fb) try{ fb.click(); }catch(e){} }
    window.aispPlayer.fullscreen=!!document.fullscreenElement;
    if(main&&document.fullscreenElement&&document.fullscreenElement.contains(main)) hideChrome(document.fullscreenElement);
    if(main){
      try{ main.loop=true; main.volume=1; main.muted=false; }catch(e){}
      // The player may run the pre-roll through this same element: while its length is the
      // ad's, the position and the wrap below must wait for the feature's.
      var featureNow=main.duration>60;
      window.aispPlayer.steps=tries;
      if(wrapped&&Date.now()-wrapped<12000&&main.paused&&!paused){ var rb=buttonWith(/^再生する$/); if(rb) try{ rb.click(); }catch(e){} else try{ var pw=main.play(); if(pw&&pw.catch) pw.catch(function(){}); }catch(e){} }
      // The player starts the feature itself once the ad is over; until it has, the element is
      // left alone so the player's own state (and the comments with it) stays in step.
      if(!main.paused) window.aispPlayer.playerStarted=true;
      var started=window.aispPlayer.playerStarted;
      // Seek once the player runs the feature; again should the element's length change under
      // it (the same element, another media).
      if(started&&featureNow&&(!seeked||Math.abs(seekedFor-main.duration)>1)&&main.readyState>=1){
        seeked=true; seekedFor=main.duration;
        var pos=startPosition(main.duration);
        window.aispPlayer.mode=pos>0?'seek':'play'; window.aispPlayer.position=pos; window.aispPlayer.seeks++;
        if(pos>0) try{ main.currentTime=pos; }catch(e){}
      }
      if(started&&seeked&&paused){ if(!main.paused){ var pb=buttonWith(/^一時停止する$/); if(pb) try{ pb.click(); }catch(e){} else try{ main.pause(); }catch(e){} } }
      else if(!adPlaying&&main.paused&&main.readyState>=2&&tries>10){ var b=buttonWith(/^再生する$/); if(b) try{ b.click(); }catch(e){} else try{ var p=main.play(); if(p&&p.catch) p.catch(function(){}); }catch(e){} }
    }
    var t=document.title||'';
    if(/ - ニコニコ動画$/.test(t)) document.title=t.replace(/ - ニコニコ動画$/,'');
    // Should the page have moved to another video regardless (a navigation inside the page,
    // which the host does not see), come back to this one, timeline and all.
    if(location.pathname!==ownPath){ window.aispPlayer.strayed=(window.aispPlayer.strayed||0)+1; location.replace(ownUrl); return; }
    // For the life of the page: the player may rebuild its elements at any time.
    setTimeout(step, main&&seeked?(main.duration-main.currentTime<3?200:2000):500);
  }
  step();
})();
