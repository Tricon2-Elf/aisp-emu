// Run by the launcher hook's off-screen browser in a Nico Live watch page
// (https://live.nicovideo.jp/watch/lv…), the nne:lv… source: the page cannot be framed
// (X-Frame-Options), so it is loaded whole and this presses the stream player's own
// フルスクリーン button, a page-level layout (the button's label flips to フルスクリーン解除, no
// Fullscreen API) that scales the player to the viewport; the viewport is the video box, so the
// box shows the stream alone. The player's header and its footer (controls, the comment form)
// stay laid over the video, since nothing ever moves a mouse here, so they are hidden; Nico's
// own flying comments stay. Anything else that plays on the page (the ad player at the top)
// is muted and paused so only the stream is heard. The page renders late, so this keeps
// looking for a while, and again after a layout change.
(function(){
  if(window.__aispNicoLive) return;
  window.__aispNicoLive=true;
  var style=document.createElement('style');
  style.textContent='[class*="player-display-header"],[class*="player-display-footer-area"]{display:none!important}';
  (document.head||document.documentElement).appendChild(style);
  function streamVideo(){
    var playing=[].slice.call(document.querySelectorAll('video')).filter(function(v){ return v.currentSrc&&!v.paused; });
    // The stream player is the largest playing one; the ad is a small one near the top.
    playing.sort(function(a,b){ var ra=a.getBoundingClientRect(), rb=b.getBoundingClientRect(); return rb.width*rb.height-ra.width*ra.height; });
    return playing[0]||null;
  }
  function quietOthers(keep){
    [].slice.call(document.querySelectorAll('video')).forEach(function(v){
      if(v===keep) return;
      try{ v.muted=true; v.pause(); }catch(e){}
    });
  }
  function fullscreenButton(){
    var all=[].slice.call(document.querySelectorAll('button'));
    for(var i=0;i<all.length;i++){ var l=all[i].getAttribute('aria-label')||''; if(/フルスクリーン/.test(l)) return all[i]; }
    return null;
  }
  var tries=0;
  function step(){
    tries++;
    var v=streamVideo();
    if(v) quietOthers(v);
    var b=fullscreenButton();
    var full=!!b&&/解除/.test(b.getAttribute('aria-label')||'');
    if(b&&!full){ try{ b.click(); }catch(e){} }
    if(tries<90) setTimeout(step, full?2000:500);
  }
  step();
})();
