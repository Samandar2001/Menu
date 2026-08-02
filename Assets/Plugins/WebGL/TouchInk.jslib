mergeInto(LibraryManager.library, {
  // Pointer events orqali chizish nuqtalarini bevosita o'qiydi (touch + sichqoncha + pen).
  // Har nuqta "x,y,d;" ko'rinishida to'planadi (x,y = Unity ekran koordinatasi, d = bosilganmi).
  TouchInk_Init: function () {
    if (window.__tinkInit) return;
    window.__tinkInit = true;
    window.__tinkBuf = "";
    var canvas = document.querySelector("#unity-canvas") || document.getElementsByTagName("canvas")[0];
    if (!canvas) { window.__tinkInit = false; return; }
    try { canvas.style.touchAction = "none"; } catch (e) {}

    function conv(cx, cy) {
      var r = canvas.getBoundingClientRect();
      var x = (cx - r.left) * (canvas.width / r.width);
      var y = canvas.height - (cy - r.top) * (canvas.height / r.height); // Unity: past-chap
      return x.toFixed(1) + "," + y.toFixed(1);
    }
    function add(cx, cy, d) { window.__tinkBuf += conv(cx, cy) + "," + d + ";"; }

    var down = false;
    canvas.addEventListener("pointerdown", function (e) {
      try { canvas.setPointerCapture(e.pointerId); } catch (x) {}
      down = true;
      add(e.clientX, e.clientY, 1);
    });
    canvas.addEventListener("pointermove", function (e) {
      if (!down) return; // bosilgan bo'lsagina (buttons/pressure'ga bog'liq emas)
      var evs = (e.getCoalescedEvents && e.getCoalescedEvents()) || null;
      if (evs && evs.length) {
        for (var i = 0; i < evs.length; i++) add(evs[i].clientX, evs[i].clientY, 1);
      } else {
        add(e.clientX, e.clientY, 1);
      }
    });
    canvas.addEventListener("pointerup", function () { down = false; window.__tinkBuf += "0,0,0;"; });
    canvas.addEventListener("pointercancel", function () { down = false; window.__tinkBuf += "0,0,0;"; });
    // sichqoncha uchun ham (desktop web)
    canvas.addEventListener("mouseleave", function () { if (down) { down = false; window.__tinkBuf += "0,0,0;"; } });
  },

  // Buferni string qilib qaytaradi va tozalaydi. Bo'sh bo'lsa "".
  TouchInk_Read: function () {
    var s = window.__tinkBuf || "";
    window.__tinkBuf = "";
    var len = lengthBytesUTF8(s) + 1;
    var buf = _malloc(len);
    stringToUTF8(s, buf, len);
    return buf;
  },

  // persistentDataPath (IDBFS) ni brauzer xotirasiga (IndexedDB) yozadi -> chizmalar saqlanib qoladi
  TouchInk_Sync: function () {
    try { if (typeof FS !== 'undefined' && FS.syncfs) FS.syncfs(false, function (e) {}); } catch (e) {}
  },

  // Telegram initData (auth uchun) — backend'га yuboriladi
  SSA_InitData: function () {
    var s = "";
    try { s = (window.Telegram && Telegram.WebApp && Telegram.WebApp.initData) || ""; } catch (e) {}
    var len = lengthBytesUTF8(s) + 1; var buf = _malloc(len); stringToUTF8(s, buf, len); return buf;
  },

  // Backend bazasi (content.json links.cabinet_api'дан /cabinet olib tashlangani) — index.html o'rnatadi
  SSA_ApiBase: function () {
    var s = window.__ssaApi || "";
    var len = lengthBytesUTF8(s) + 1; var buf = _malloc(len); stringToUTF8(s, buf, len); return buf;
  }
});
