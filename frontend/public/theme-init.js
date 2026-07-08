// Zet het thema vóór de eerste paint zodat er geen lichtflits is bij dark mode.
// Bewust een extern bestand (i.p.v. inline) zodat een strikte CSP zonder
// 'unsafe-inline' voor scripts volstaat (security-hardening L3).
(function () {
  try {
    var saved = localStorage.getItem('pharmadocs.theme')
    var theme = saved || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')
    document.documentElement.setAttribute('data-theme', theme)
  } catch (e) {
    document.documentElement.setAttribute('data-theme', 'light')
  }
})()
