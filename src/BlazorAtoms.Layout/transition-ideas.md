Transition ideas:
https://prismic.io/blog/css-animation-examples#30-3d-cards-flip
https://prismic.io/blog/css-ui-features

seperators = `===`

DONE 1. The Smooth Fade & Scale This gives an elegant, native app-like feel by having the element grow slightly as it fades in.css

.fade-scale-in {
  opacity: 0;
  transform: scale(0.95);
  transition: opacity 0.4s ease-out, transform 0.4s ease-out;
}

.fade-scale-in.visible {
  opacity: 1;
  transform: scale(1);
}

===

DONE 2. The Slide-Up RevealGreat for text blocks or cards, this mimics a theatrical curtain rising as the element slides up into view.css
.slide-up-in {
  opacity: 0;
  transform: translateY(30px);
  transition: opacity 0.6s cubic-bezier(0.16, 1, 0.3, 1), transform 0.6s cubic-bezier(0.16, 1, 0.3, 1);
}

.slide-up-in.visible {
  opacity: 1;
  transform: translateY(0);
}


===

DONE 3. The Shift & BlurBy combining a horizontal slide with a subtle blur effect, this transition creates a high-end, dynamic entrance.css
.shift-blur-in {
  opacity: 0;
  transform: translateX(-20px);
  filter: blur(5px);
  transition: opacity 0.5s ease, transform 0.5s ease, filter 0.5s ease;
}

.shift-blur-in.visible {
  opacity: 1;
  transform: translateX(0);
  filter: blur(0);
}


===

DONE 4. The 3D Card FlipPerfect for image galleries or feature sections, this flips the element on the Y-axis to reveal it.css
.flip-in {
  opacity: 0;
  transform: perspective(600px) rotateY(-20deg);
  transition: opacity 0.5s ease, transform 0.5s ease;
}

.flip-in.visible {
  opacity: 1;
  transform: perspective(600px) rotateY(0deg);
}


===

How to Trigger ThemTo trigger these animations when the user scrolls the element into view, you will need a tiny bit of JavaScript using the native Intersection Observer API:javascriptdocument.addEventListener("DOMContentLoaded", () => {
  const observer = new IntersectionObserver(entries => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        entry.target.classList.add('visible');
        observer.unobserve(entry.target); // Stop observing once animated
      }
    });
  });

  document.querySelectorAll('.fade-scale-in, .slide-up-in, .shift-blur-in, .flip-in').forEach(el => {
    observer.observe(el);
  });
});


===


1. Simple text animation

Simple CSS text animation

HTML code:
```html
<div id=container>
  Make 
  <div id=flip>
    <div><div>wOrK</div></div>
    <div><div>lifeStyle</div></div>
    <div><div>Everything</div></div>
  </div>
  AweSoMe!
</div>

<p>a css3 animation demo</p>
```
```css
@import url('https://fonts.googleapis.com/css?family=Roboto:700');

body {
  margin:0px;
  font-family:'Roboto';
  text-align:center;
}

#container {
  color:#999;
  text-transform: uppercase;
  font-size:36px;
  font-weight:bold;
  padding-top:200px;  
  position:fixed;
  width:100%;
  bottom:45%;
  display:block;
}

#flip {
  height:50px;
  overflow:hidden;
}

#flip > div > div {
  color:#fff;
  padding:4px 12px;
  height:45px;
  margin-bottom:45px;
  display:inline-block;
}

#flip div:first-child {
  animation: show 5s linear infinite;
}

#flip div div {
  background:#42c58a;
}
#flip div:first-child div {
  background:#4ec7f3;
}
#flip div:last-child div {
  background:#DC143C;
}

@keyframes show {
  0% {margin-top:-270px;}
  5% {margin-top:-180px;}
  33% {margin-top:-180px;}
  38% {margin-top:-90px;}
  66% {margin-top:-90px;}
  71% {margin-top:0px;}
  99.99% {margin-top:0px;}
  100% {margin-top:-270px;}
}

p {
  position:fixed;
  width:100%;
  bottom:30px;
  font-size:12px;
  color:#999;
  margin-top:200px;
}
```
The simple text animation creates a vertical text-flipping effect inside the #flip container, making different words — work, lifestyle, and everything — appear in a sequential and infinite loop. The animation is controlled by the @keyframes show (docs), which adjusts the margin-top of the child div elements, causing them to slide into view.


===


2. CSS text animation

Pure CSS Text animation

```html
<div class="container">
  <!--<div class="take-input">
     <input type="text" placeholder="Write any text" />
    <a href="javascript:void(0);">Enter Text</a>
  </div>-->
  <div class="animate seven">
    <span>m</span><span>a</span><span>m</span><span>u</span><span>n</span>&nbsp;<span>k</span><span>h</span><span>a</span><span>n</span><span>d</span><span>a</span><span>k</span><span>e</span><span>r</span>
      
      <a class="repeat" href="javascript:void(0);">Repeat Animation</a>
		</div>
  
		<div class="animate one">
			<span>c</span><span>s</span><span>s</span><span>3</span> &nbsp;
			<span>a</span><span>n</span><span>i</span><span>m</span><span>a</span><span>t</span><span>i</span><span>o</span><span>n</span><span>s</span>
      
      <a class="repeat" href="javascript:void(0);">Repeat Animation</a>
		</div>

		<div class="animate two">
			<span>c</span><span>s</span><span>s</span><span>3</span> &nbsp;
			<span>a</span><span>n</span><span>i</span><span>m</span><span>a</span><span>t</span><span>i</span><span>o</span><span>n</span><span>s</span>
      
      <a class="repeat" href="javascript:void(0);">Repeat Animation</a>
		</div>

		<div class="animate three">
			<span>c</span><span>s</span><span>s</span><span>3</span> &nbsp;
			<span>a</span><span>n</span><span>i</span><span>m</span><span>a</span><span>t</span><span>i</span><span>o</span><span>n</span><span>s</span>
      
      <a class="repeat" href="javascript:void(0);">Repeat Animation</a>
		</div>

		<div class="animate four">
			<span>c</span><span>s</span><span>s</span><span>3</span> &nbsp;
			<span>a</span><span>n</span><span>i</span><span>m</span><span>a</span><span>t</span><span>i</span><span>o</span><span>n</span><span>s</span>
      
      <a class="repeat" href="javascript:void(0);">Repeat Animation</a>
		</div>

		<div class="animate five">
			<span>c</span><span>s</span><span>s</span><span>3</span> &nbsp;
			<span>a</span><span>n</span><span>i</span><span>m</span><span>a</span><span>t</span><span>i</span><span>o</span><span>n</span><span>s</span>
      
      <a class="repeat" href="javascript:void(0);">Repeat Animation</a>
		</div>

		<div class="animate six">
			<span>c</span><span>s</span><span>s</span><span>3</span> &nbsp;
			<span>a</span><span>n</span><span>i</span><span>m</span><span>a</span><span>t</span><span>i</span><span>o</span><span>n</span><span>s</span>
      
      <a class="repeat" href="javascript:void(0);">Repeat Animation</a>
		</div>
	</div>
```
```css
@import url('https://fonts.googleapis.com/css?family=Lato:100,100i,300,300i,400,400i,700,700i,900,900i');

*
{
	margin: 0;
	padding: 0;
}

body
{
	font-family: 'Lato', sans-serif;
	font-size: 14px;
	color: #999999;
	word-wrap:break-word;
}

p
{
	margin: 0 0 10px;
}

ul
{
	list-style: none;
}

.container {
	width: 100%;
	margin: auto;
	font-weight: 900;
	text-transform: uppercase;
	text-align: center;
	padding: 0 0 200px;
}

/*.take-input {
  margin: 50px 0 0;
}

.take-input input {
  width: 400px;
  height: 35px;
  padding: 0 10px;
  border-radius: 5px;
  border: 1px solid #ececec;
  margin: 0 15px 0 0;
  font-size: 15px;
}*/

a, a:link, a:visited {
  text-decoration: none;
  padding: 9px 15px;
  border: 1px solid #ececec;
  border-radius: 5px;
  color: gray;
}

.animate {
	font-size: 50px;
	margin: 100px 0 0;
	border-bottom: 2px solid #ccc;
}

.animate span {
	display: inline-block;
}

a.repeat {
  display: inline-block;
  font-size: 12px;
  text-transform: none;
  text-decoration: none;
  color: orange;
  padding: 5px 12px;
  border: 1px solid rgba(0, 0, 0, 0.15);
  font-weight: normal;
  margin: 0 0 0 50px;
  border-radius: 3px;
  position: relative;
  bottom: 15px;
}

a.repeat:hover {
  background: rgba(0, 0, 0, 0.7);
  color: white;
}

.animate span:nth-of-type(2) {
	animation-delay: .05s;
}
.animate span:nth-of-type(3) {
	animation-delay: .1s;
}
.animate span:nth-of-type(4) {
	animation-delay: .15s;
}
.animate span:nth-of-type(5) {
	animation-delay: .2s;
}
.animate span:nth-of-type(6) {
	animation-delay: .25s;
}
.animate span:nth-of-type(7) {
	animation-delay: .3s;
}
.animate span:nth-of-type(8) {
	animation-delay: .35s;
}
.animate span:nth-of-type(9) {
	animation-delay: .4s;
}
.animate span:nth-of-type(10) {
	animation-delay: .45s;
}
.animate span:nth-of-type(11) {
	animation-delay: .5s;
}
.animate span:nth-of-type(12) {
	animation-delay: .55s;
}
.animate span:nth-of-type(13) {
	animation-delay: .6s;
}
.animate span:nth-of-type(14) {
	animation-delay: .65s;
}
.animate span:nth-of-type(15) {
	animation-delay: .7s;
}
.animate span:nth-of-type(16) {
	animation-delay: .75s;
}
.animate span:nth-of-type(17) {
	animation-delay: .8s;
}
.animate span:nth-of-type(18) {
	animation-delay: .85s;
}
.animate span:nth-of-type(19) {
	animation-delay: .9s;
}
.animate span:nth-of-type(20) {
	animation-delay: .95s;
}


/* Animation One */

.one span {
	color: #24a8e6;
	opacity: 0;
	transform: translate(-150px, -50px) rotate(-180deg) scale(3);
	animation: revolveScale .4s forwards;
}

@keyframes revolveScale {
	60% {
		transform: translate(20px, 20px) rotate(30deg) scale(.3);
	}

	100% {
		transform: translate(0) rotate(0) scale(1);
		opacity: 1;
	}
}


/* Animation Two */

.two span {
	color: #a5cb21;
	opacity: 0;
	transform: translate(200px, -100px) scale(2);
	animation: ballDrop .3s forwards;
}

@keyframes ballDrop {
	60% {
		transform: translate(0, 20px) rotate(-180deg) scale(.5);
	}

	100% {
		transform: translate(0) rotate(0deg) scale(1);
		opacity: 1;
	}
}


/* Animation Three */


.three span {
	color: #b10e81;
	opacity: 0;
	transform: translate(-300px, 0) scale(0);
	animation: sideSlide .5s forwards;
}

@keyframes sideSlide {
	60% {
		transform: translate(20px, 0) scale(1);
		color: #b10e81;
	}

	80% {
		transform: translate(20px, 0) scale(1);
		color: #b10e81;
	}

	99% {
		transform: translate(0) scale(1.2);
		color: #00f0ff;
	}

	100% {
		transform: translate(0) scale(1);
		opacity: 1;
		color: #b10e81;
	}
}


/* Animation Four */


.four span {
	color: #8d6a00;
	opacity: 0;
	transform: translate(0, -100px) rotate(360deg) scale(0);
	animation: revolveDrop .3s forwards;
}


@keyframes revolveDrop {
	30% {
		transform: translate(0, -50px) rotate(180deg) scale(1);
	}

	60% {
		transform: translate(0, 20px) scale(.8) rotate(0deg);
	}

	100% {
		transform: translate(0) scale(1) rotate(0deg);
		opacity: 1;
	}
}


/* Animation Five */


.five span {
	color: #dd3f0f;
	opacity: 0;
	transform: translate(0, -100px) rotate(360deg) scale(0);
	animation: dropVanish .5s forwards;
}


@keyframes dropVanish {
	30% {
		transform: translate(0, -50px) rotate(180deg) scale(1);
	}

	50% {
		transform: translate(0, 20px) scale(.8) rotate(0deg);
		opacity: 1;
	}

	80% {
		transform: translate(-100px, -100px) scale(1.5) rotate(-180deg);
		opacity: 0;
	}

	100% {
		transform: translate(0) scale(1) rotate(0deg);
		opacity: 1;
	}
}



/* Animation Six */


.six span {
	color: #ddb40f;
	opacity: 0;
	transform: rotate(-180deg) translate(150px, 0);
	animation: twister .5s forwards;
}


@keyframes twister {
	10% {
		opacity: 1;
	}
	100% {
		transform: rotate(0deg) translate(0);
		opacity: 1;
	}
}



/* Animation Seven */


.seven span {
	color: #348c04;
	opacity: 0;
	transform: translate(-150px, 0) scale(.3);
	animation: leftRight .5s forwards;
}


@keyframes leftRight {
	40% {
		transform: translate(50px, 0) scale(.7);
		opacity: 1;
		color: #348c04;
	}

	60% {
		color: #0f40ba;
	}

	80% {
		transform: translate(0) scale(2);
		opacity: 0;
	}

	100% {
		transform: translate(0) scale(1);
		opacity: 1;
	}
}
```
```javascript
/* This code is not required for the animation. This is only needed for the repeatation */

$(function(){
	$('.repeat').click(function(){
    	var classes =  $(this).parent().attr('class');
        $(this).parent().attr('class', 'animate');
        var indicator = $(this);
        setTimeout(function(){ 
        	$(indicator).parent().addClass(classes);
        }, 20);
    });
});

```


The CSS text animation creates a series of animations — rotation, scaling, bouncing, sliding in, and various combinations — for different groups of text. The animations are powered by @keyframes declarations that control transformations like translate (docs), scale (docs), and opacity (docs).

While this animation doesn’t require JavaScript to work, JavaScript is used to repeat the animations when the “Repeat Animation” button is clicked.



===


3. Hover effect for headers

Hover Effect for Headers


The hover effect for headers by Olivia Ng creates a visually engaging webpage with sections representing different natural elements like deserts, forests, canyons, glaciers, mountains, oceans, and more. Each "element" has a unique background image, interactive hover effects, and text animations to enhance user experience.

4. CSS party5.

CSS Party

```html
<main>
  <article>
    <h1 split-by="letter" letter-animation="breath">2024</h1>
    <h2>The year of CSS</h2>
  </article>
  <section>
    <div>

      <span>Party</span>
      <span>Chill</span>
      <input min="100" max="2400" type="range" />
    </div>
  </section>
</main>
```
```css
html,
body,
main {
  --font: "skolar-sans-latin", sans-serif;
  --really-green: oklch(88.14% 0.203 158.82);
  --green: oklch(78.59% 0.18 159.1);
  --black: oklch(22.21% 0 0);
  --gray: oklch(83.9% 0 0);
  --speed: 1200ms;

  height: 100%;
  font-family: var(--font);
  background-color: var(--black);
  color: var(--gray);

  & * {
    font-family: inherit;
  }
}

main {
  display: grid;
  place-content: center;
  place-items: center;
  text-align: center;
  gap: 88px;

  & article {
    display: grid;
    gap: 52px;
  }

  & h1 {
    color: var(--green);
    font-size: 14vw;
    font-weight: 900;
  }

  & h2 {
    font-size: 4.5vw;
    text-transform: uppercase;
    letter-spacing: 0.025ch;
  }
}

*[letter-animation="breath"] {
  & > span {
    display: inline-block;
    white-space: break-spaces;
  }

  & > span {
    animation: breath calc(var(--speed)) ease calc(var(--index) * 100 * 1ms)
      infinite alternate;
  }
}

section {
  width: 100%;
  display: grid;
  place-content: center;
  place-items: center;

  & div {
    width: 50vw;

    & span {
      text-transform: uppercase;
      font-weight: bold;
      letter-spacing: 0.025ch;
      position: relative;
      margin-block-end: 8px;
    }

    & span:first-of-type {
      float: left;
      left: -12px;
    }

    & span:last-of-type {
      float: right;
      right: -12px;
    }
  }

  & input[type="range"] {
    width: 100%;
  }
}

input[type="range"] {
  -webkit-appearance: none;
  appearance: none;
  background: transparent;
  cursor: pointer;
}

input[type="range"]::-webkit-slider-runnable-track {
  background: var(--green);
  height: 0.5rem;
  border-radius: 12px;
}

input[type="range"]::-webkit-slider-thumb {
  -webkit-appearance: none; /* Override default look */
  appearance: none;
  margin-top: -12px;
  background-color: oklch(22.21% 0 0);
  border: 1px solid oklch(38% 0 0);
  border-radius: 8px;
  height: 2rem;
  width: 1.25rem;
}

@keyframes breath {
  from {
    animation-timing-function: ease-out;
  }
  to {
    transform: scale(1.25) translateY(-5px) perspective(1px);
    text-shadow: 0 0 20px var(--really-green);
    animation-timing-function: ease-in-out;
  }
}

```
```javascript
const slider = document.querySelector('input[type="range"]');
const root = document.querySelector("article");

const span = (text, index) => {
  const node = document.createElement("span");

  node.textContent = text;
  node.style.setProperty("--index", index);

  return node;
};

export const byLetter = (text) => [...text].map(span);

export const byWord = (text) => text.split(" ").map(span);

const { matches: motionOK } = window.matchMedia(
  "(prefers-reduced-motion: no-preference)"
);

if (motionOK) {
  const splitTargets = document.querySelectorAll("[split-by]");

  splitTargets.forEach((node) => {
    const type = node.getAttribute("split-by");
    let nodes = null;

    if (type === "letter") nodes = byLetter(node.innerText);
    else if (type === "word") nodes = byWord(node.innerText);

    if (nodes) node.firstChild.replaceWith(...nodes);
  });
}

function onInput() {
  const value = slider.value;
  root.style.setProperty("--speed", `${value}ms`);
}

slider.addEventListener("input", debounce(onInput, 10));

function debounce(func, wait) {
  let timeoutId;
  return function () {
    const context = this;
    const args = arguments;
    clearTimeout(timeoutId);
    timeoutId = setTimeout(() => {
      func.apply(context, args);
    }, wait);
  };
}

```

The CSS party animation by David East puts control in your hands by providing a slider for controlling the intensity and speed of the text animation.

This text animation relies on @keyframes breath, which adds a text shadow to the text and makes it scale up and down, and some JavaScript code that handles the slider’s input value and uses CSS custom properties to control the animation’s timing.

See more creative text animations!
Want to see more examples of CSS text animations? Then, check out our dedicated article that covers 40 text effects.


===


# Hover animations

===


5. Animated CSS mail button

Animated CSS Mail Button

```html
<div class="letter-image">
	<div class="animated-mail">
		<div class="back-fold"></div>
		<div class="letter">
			<div class="letter-border"></div>
			<div class="letter-title"></div>
			<div class="letter-context"></div>
			<div class="letter-stamp">
				<div class="letter-stamp-inner"></div>
			</div>
		</div>
		<div class="top-fold"></div>
		<div class="body"></div>
		<div class="left-fold"></div>
	</div>
	<div class="shadow"></div>
</div>
```
```scss
body {
	background: #323641;
}

.letter-image {
	position: absolute;
	top: 50%;
	left: 50%;
	width: 200px;
	height: 200px;
	-webkit-transform: translate(-50%, -50%);
	-moz-transform: translate(-50%, -50%);
	transform: translate(-50%, -50%);
	cursor: pointer;
}

.animated-mail {
	position: absolute;
	height: 150px;
	width: 200px;
	-webkit-transition: .4s;
	-moz-transition: .4s;
	transition: .4s;
	
	.body {
		position: absolute;
		bottom: 0;
		width: 0;
		height: 0;
		border-style: solid;
		border-width: 0 0 100px 200px;
		border-color: transparent transparent #e95f55 transparent;
		z-index: 2;
	}
	
	.top-fold {
		position: absolute;
		top: 50px;
		width: 0;
		height: 0;
		border-style: solid;
		border-width: 50px 100px 0 100px;
		-webkit-transform-origin: 50% 0%;
		-webkit-transition: transform .4s .4s, z-index .2s .4s;
		-moz-transform-origin: 50% 0%;
		-moz-transition: transform .4s .4s, z-index .2s .4s;
		transform-origin: 50% 0%;
		transition: transform .4s .4s, z-index .2s .4s;
		border-color: #cf4a43 transparent transparent transparent;
		z-index: 2;
	}
	
	.back-fold {
		position: absolute;
		bottom: 0;
		width: 200px;
		height: 100px;
		background: #cf4a43;
		z-index: 0;
	}
	
	.left-fold {
		position: absolute;
		bottom: 0;
		width: 0;
		height: 0;
		border-style: solid;
		border-width: 50px 0 50px 100px;
		border-color: transparent transparent transparent #e15349;
		z-index: 2;
	}
	
	.letter {
		left: 20px;
		bottom: 0px;
		position: absolute;
		width: 160px;
		height: 60px;
		background: white;
		z-index: 1;
		overflow: hidden;
		-webkit-transition: .4s .2s;
		-moz-transition: .4s .2s;
		transition: .4s .2s;
		
		.letter-border {
			height: 10px;
			width: 100%;
      background: repeating-linear-gradient(
        -45deg,
        #cb5a5e,
        #cb5a5e 8px,
        transparent 8px,
        transparent 18px
      );
		}
		
		.letter-title {
			margin-top: 10px;
			margin-left: 5px;
			height: 10px;
			width: 40%;
			background: #cb5a5e;
		}
		.letter-context {
			margin-top: 10px;
			margin-left: 5px;
			height: 10px;
			width: 20%;
			background: #cb5a5e;
		}
		
		.letter-stamp {
			margin-top: 30px;
			margin-left: 120px;
			border-radius: 100%;
			height: 30px;
			width: 30px;
			background: #cb5a5e;
			opacity: 0.3;
		}
	}
}

.shadow {
	position: absolute;
	top: 200px;
	left: 50%;
	width: 400px;
	height: 30px;
	transition: .4s;
	transform: translateX(-50%);
	-webkit-transition: .4s;
	-webkit-transform: translateX(-50%);
	-moz-transition: .4s;
	-moz-transform: translateX(-50%);
	
	border-radius: 100%;
	background: radial-gradient(rgba(0,0,0,0.5), rgba(0,0,0,0.0), rgba(0,0,0,0.0));
}

	.letter-image:hover {
		.animated-mail {
			transform: translateY(50px);
			-webkit-transform: translateY(50px);
			-moz-transform: translateY(50px);
		}
		
		.animated-mail .top-fold {
			transition: transform .4s, z-index .2s;
			transform: rotateX(180deg);
			-webkit-transition: transform .4s, z-index .2s;
			-webkit-transform: rotateX(180deg);
			-moz-transition: transform .4s, z-index .2s;
			-moz-transform: rotateX(180deg);
			z-index: 0;
		}
		
		.animated-mail .letter {
			height: 180px;
		}
		
		.shadow {
			width: 250px;
		}
	}
```

The animated CSS main button by Jake Giles-Phillips creates an envelope animation where the envelope opens and reveals its content on hover. It relies on various CSS transforms to work.


===


6. Sparkly shiny text

Sparkly Shiny Text ✨

```html
<a href="#">
      <!-- Drop as many sparkles in as you need and scope the speed -->
      <svg viewBox="0 0 96 96" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path
          d="M93.781 51.578C95 50.969 96 49.359 96 48c0-1.375-1-2.969-2.219-3.578 0 0-22.868-1.514-31.781-10.422-8.915-8.91-10.438-31.781-10.438-31.781C50.969 1 49.375 0 48 0s-2.969 1-3.594 2.219c0 0-1.5 22.87-10.406 31.781-8.908 8.913-31.781 10.422-31.781 10.422C1 45.031 0 46.625 0 48c0 1.359 1 2.969 2.219 3.578 0 0 22.873 1.51 31.781 10.422 8.906 8.911 10.406 31.781 10.406 31.781C45.031 95 46.625 96 48 96s2.969-1 3.562-2.219c0 0 1.523-22.871 10.438-31.781 8.913-8.908 31.781-10.422 31.781-10.422Z"
          fill="#000"
        />
      </svg>
      <svg viewBox="0 0 96 96" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path
          d="M93.781 51.578C95 50.969 96 49.359 96 48c0-1.375-1-2.969-2.219-3.578 0 0-22.868-1.514-31.781-10.422-8.915-8.91-10.438-31.781-10.438-31.781C50.969 1 49.375 0 48 0s-2.969 1-3.594 2.219c0 0-1.5 22.87-10.406 31.781-8.908 8.913-31.781 10.422-31.781 10.422C1 45.031 0 46.625 0 48c0 1.359 1 2.969 2.219 3.578 0 0 22.873 1.51 31.781 10.422 8.906 8.911 10.406 31.781 10.406 31.781C45.031 95 46.625 96 48 96s2.969-1 3.562-2.219c0 0 1.523-22.871 10.438-31.781 8.913-8.908 31.781-10.422 31.781-10.422Z"
          fill="#000"
        />
      </svg>
      <svg viewBox="0 0 96 96" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path
          d="M93.781 51.578C95 50.969 96 49.359 96 48c0-1.375-1-2.969-2.219-3.578 0 0-22.868-1.514-31.781-10.422-8.915-8.91-10.438-31.781-10.438-31.781C50.969 1 49.375 0 48 0s-2.969 1-3.594 2.219c0 0-1.5 22.87-10.406 31.781-8.908 8.913-31.781 10.422-31.781 10.422C1 45.031 0 46.625 0 48c0 1.359 1 2.969 2.219 3.578 0 0 22.873 1.51 31.781 10.422 8.906 8.911 10.406 31.781 10.406 31.781C45.031 95 46.625 96 48 96s2.969-1 3.562-2.219c0 0 1.523-22.871 10.438-31.781 8.913-8.908 31.781-10.422 31.781-10.422Z"
          fill="#000"
        />
      </svg>
      <svg viewBox="0 0 96 96" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path
          d="M93.781 51.578C95 50.969 96 49.359 96 48c0-1.375-1-2.969-2.219-3.578 0 0-22.868-1.514-31.781-10.422-8.915-8.91-10.438-31.781-10.438-31.781C50.969 1 49.375 0 48 0s-2.969 1-3.594 2.219c0 0-1.5 22.87-10.406 31.781-8.908 8.913-31.781 10.422-31.781 10.422C1 45.031 0 46.625 0 48c0 1.359 1 2.969 2.219 3.578 0 0 22.873 1.51 31.781 10.422 8.906 8.911 10.406 31.781 10.406 31.781C45.031 95 46.625 96 48 96s2.969-1 3.562-2.219c0 0 1.523-22.871 10.438-31.781 8.913-8.908 31.781-10.422 31.781-10.422Z"
          fill="#000"
        />
      </svg>
      <svg viewBox="0 0 96 96" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path
          d="M93.781 51.578C95 50.969 96 49.359 96 48c0-1.375-1-2.969-2.219-3.578 0 0-22.868-1.514-31.781-10.422-8.915-8.91-10.438-31.781-10.438-31.781C50.969 1 49.375 0 48 0s-2.969 1-3.594 2.219c0 0-1.5 22.87-10.406 31.781-8.908 8.913-31.781 10.422-31.781 10.422C1 45.031 0 46.625 0 48c0 1.359 1 2.969 2.219 3.578 0 0 22.873 1.51 31.781 10.422 8.906 8.911 10.406 31.781 10.406 31.781C45.031 95 46.625 96 48 96s2.969-1 3.562-2.219c0 0 1.523-22.871 10.438-31.781 8.913-8.908 31.781-10.422 31.781-10.422Z"
          fill="#000"
        />
      </svg>
      <!-- End sparkles -->
      <span>Click!</span>
      <span aria-hidden="true">Click!</span>
    </a>
```
```css
:root {
  --color: var(--yellow-5);
  --shadow: var(--yellow-8);
  --glare: hsl(0 0% 100% / 0.75);
  --font-size: var(--font-size-fluid-3);
  --transition: 0.2s;
}

* {
  box-sizing: border-box;
}

body {
  display: grid;
  place-items: center;
  min-height: 100vh;
  font-family: 'Google Sans', sans-serif, system-ui;
  background: var(--gray-9);
}

a {
  --padding: var(--size-2);
  padding: var(--padding);
  border-radius: var(--size-4);
  text-decoration: none;
  color: transparent;
  position: relative;
  transition: background 0.2s;
}

a:hover {
  background: var(--gray-8);
}

span {
  display: inline-block;
  font-size: calc(var(--font-size) * 1.5);
  font-weight: var(--font-weight-9);
  transition: all 0.2s;
  text-decoration: none;
  text-shadow:
    calc(var(--hover) * (var(--font-size) * -0)) calc(var(--hover) * (var(--font-size) * 0)) var(--shadow),
    calc(var(--hover) * (var(--font-size) * -0.02)) calc(var(--hover) * (var(--font-size) * 0.02)) var(--shadow),
    calc(var(--hover) * (var(--font-size) * -0.04)) calc(var(--hover) * (var(--font-size) * 0.04)) var(--shadow),
    calc(var(--hover) * (var(--font-size) * -0.06)) calc(var(--hover) * (var(--font-size) * 0.06)) var(--shadow),
    calc(var(--hover) * (var(--font-size) * -0.08)) calc(var(--hover) * (var(--font-size) * 0.08)) var(--shadow),
    calc(var(--hover) * (var(--font-size) * -0.10)) calc(var(--hover) * (var(--font-size) * 0.10)) var(--shadow);
/*     calc(var(--hover) * -2px) calc(var(--hover) * 2px) var(--shadow),
    calc(var(--hover) * -3px) calc(var(--hover) * 3px) var(--shadow),
    calc(var(--hover) * -4px) calc(var(--hover) * 4px) var(--shadow), */
/*     calc(var(--hover) * -5px) calc(var(--hover) * 5px) var(--shadow); */
  transform: translate(calc(var(--hover) * (var(--font-size) * 0.10)), calc(var(--hover) * (var(--font-size) * -0.10)));
}

span:last-of-type {
  position: absolute;
  inset: var(--padding);
  background: linear-gradient(
    108deg,
    transparent 0 55%,
    var(--glare) 55% 60%,
    transparent 60% 70%,
    var(--glare) 70% 85%,
    transparent 85%
  ) calc(var(--pos) * -200%) 0% / 200% 100%, var(--color);
  -webkit-background-clip: text;
  color: transparent;
  z-index: 2;
  text-shadow: none;
  transform: translate(calc(var(--hover) * (var(--font-size) * 0.10)), calc(var(--hover) * (var(--font-size) * -0.10)));
}

span:last-of-type {
  transition: transform 0.2s, background-position 0s;
}

a:hover span:last-of-type {
  transition: transform 0.2s, background-position calc(var(--hover) * 1.5s) calc(var(--hover) * 0.25s);
}

a {
  --hover: 0.4;
  --pos: 0;
}

a:hover {
  --hover: 1;
  --pos: 1;
}

a:active {
  --hover: 0;
}

a:active span:last-of-type {
  --hover: 0;
  --pos: 1;
}

a svg {
	position: absolute;
	z-index: 3;
	width: calc(var(--font-size) * 0.5);
	aspect-ratio: 1;
}

a svg path {
	fill: var(--glare);
}

/* Animation for sparkles */

a:hover svg {
	animation: sparkle 0.75s calc((var(--delay-step) * var(--d)) * 1s) both;
}

@keyframes sparkle {
	50% {
		transform: translate(-50%, -50%) scale(var(--s, 1));
	}
}

a svg {
	--delay-step: 0.15;
	top: calc(var(--y, 50) * 1%);
	left:  calc(var(--x, 0) * 1%);
	transform: translate(-50%, -50%) scale(0);
}

a svg:nth-of-type(1) {
	--x: 0;
	--y: 20;
	--s: 1.1;
	--d: 1;
}

a svg:nth-of-type(2) {
	--x: 15;
	--y: 80;
	--s: 1.25;
	--d: 2;
}

a svg:nth-of-type(3) {
	--x: 45;
	--y: 40;
	--s: 1.1;
	--d: 3;
}

a svg:nth-of-type(4) {
	--x: 75;
	--y: 60;
	--s: 0.9;
	--d: 2;
}

a svg:nth-of-type(5) {
	--x: 100;
	--y: 30;
	--s: 0.8;
	--d: 4;
}

```

The sparkly shiny text animation creates an interactive button that features multiple black sparkle SVGs that animate on hover. The text of the button has a layered effect that creates a text-shadow (docs) for a 3D-like appearance.

===


7. Glowing dot effect

Full CSS growing dot effect

```html
.card
  .card__img
    %img{"src" => "https://blog.codepen.io/wp-content/uploads/2012/06/Button-Fill-Black-Large.png"}
    .card__grid-effect
      -(1..100).each do |tile|
        %a{:class => "card__grid-effect-tile", "href" => "#"}
  
    
```
```scss
html,body{
  height: 100%;
}
body{
  background-color: #A9C9FF;
background-image: linear-gradient(180deg, #A9C9FF 0%, #FFBBEC 100%);
  background-size: 100% 100%;
  display:flex;
  align-items:center;
  justify-content:center;
}
.card{
  background: #fff;
  border-radius: 2rem;
  box-shadow:0 0 10rem -5rem;
  overflow: hidden;
  &__img{
    position: relative;
    height: 30rem;
    width: 30rem;
    display:flex;
    align-items:center;
    justify-content:center;
    > img {
        width: 10rem;
        position: relative;
        z-index: 1;
        pointer-events:none;
    }
  }
  &__grid-effect{
    position: absolute;
    z-index: 0;
    inset: 0;
    display: grid;
    grid-template-columns: repeat(10, 1fr);
    grid-template-rows: repeat(10, 1fr);
    &-tile{
        position: relative;
        &:before{
          content: '';
          color: #A9C9FF;
          position: absolute;
          top: 50%;
          left: 50%;
          transform: translate(-50%, -50%);
          height: 0.3rem;
          width: 0.3rem;
          border-radius: 50%;
          background: #A9C9FF;
          transition: 500ms linear all;
          $bxs: 0 0 0;
          $gap: 3rem;
          $coef: -0.3rem;
          @for $i from 1 through 4 {
            $bxs: $bxs + #{','}$i * $gap 0 0 $i *$coef#{','}$i * -$gap 0 0 $i *$coef#{','} 0 $i *-$gap 0 $i *$coef#{','} 0 $i *$gap 0 $i *$coef;
            
            @for $j from 1 through 4 {
              $bxs: $bxs + #{','}$i * $gap $j * $gap 0 $i*$j*1.5 *$coef#{','}$i * $gap $j * -$gap 0 $i*$j*1.5 *$coef#{','}$i * -$gap $j * $gap 0 $i*$j*1.5 *$coef#{','}$i * -$gap $j * -$gap 0 $i*$j*1.5 *$coef;
            }
          }
          box-shadow: $bxs;
        }
      &:hover{
        &:before{
          height: 3rem;
          width: 3rem;
          transition: 70ms linear all; 
        }
      }
    }
  }
}
```
The glowing dot effect by Vincent Durand creates a card component with a grid of tiny dots as its background. The dots expand as users hover over specific sections of the card, and this makes for an interesting effect.

===


8. Glide to reveal

Glide To Reveal with CSS :has()

```html
<section>
  <p>Glide To Reveal Secret Code</p>
  <ul class="code">
    <li tabindex="0" class="digit">
      <span>0</span>
    </li>
    <li tabindex="0" class="digit">
      <span>3</span>
    </li>
    <li tabindex="0" class="digit">
      <span>4</span>
    </li>
    <li tabindex="0" class="digit">
      <span>8</span>
    </li>
    <li tabindex="0" class="digit">
      <span>7</span>
    </li>
    <li tabindex="0" class="digit">
      <span>2</span>
    </li>
  </ul>
</section>
```
```css
* {
  box-sizing: border-box;
}

body {
  min-height: 100vh;
  display: grid;
  place-items: center;
  font-family: "SF Pro Text", "SF Pro Icons", "AOS Icons", "Helvetica Neue", Helvetica, Arial, sans-serif, system-ui;
  background: hsl(0 0% 0%);
  gap: 2rem;
}

body::before {
	--line: hsl(0 0% 95% / 0.25);
	content: "";
	height: 100vh;
	width: 100vw;
	position: fixed;
	background:
		linear-gradient(90deg, var(--line) 1px, transparent 1px 10vmin) 0 -5vmin / 10vmin 10vmin,
		linear-gradient(var(--line) 1px, transparent 1px 10vmin) 0 -5vmin / 10vmin 10vmin;
	mask: linear-gradient(-15deg, transparent 30%, white);
	top: 0;
	z-index: -1;
}

section {
  display: grid;
  gap: 4rem;
  align-items: center;
  justify-content: center;
}

section p {
  margin: 0;
  font-size: 2.25rem;
  color: hsl(0 0% 40%);
  text-align: center;
  background: linear-gradient(hsl(0 0% 80%), hsl(0 0% 50%));
  color: transparent;
  background-clip: text;
}

.code {
  font-size: 3rem;
  display: flex;
  flex-wrap: nowrap;
  color: hsl(0 0% 100%);
  border-radius: 1rem;
  background: hsl(0 0% 6%);
  justify-content: center;
  box-shadow: 0 1px hsl(0 0% 100% / 0.25) inset;
}

.code:hover {
  cursor: grab;
}

.digit {
  display: flex;
  height: 100%;
  padding: 5.5rem 1rem;
}

.digit:focus-visible {
  outline-color: hsl(0 0% 50% / 0.25);
  outline-offset: 1rem;
}

.digit span {
  scale: calc(var(--active, 0) + 0.5);
  filter: blur(calc((1 - var(--active, 0)) * 1rem));
  transition: scale calc(((1 - var(--active, 0)) + 0.2) * 1s), filter calc(((1 - var(--active, 0)) + 0.2) * 1s);
}

ul {
  padding: 0;
  margin: 0;
}

.digit:first-of-type {
  padding-left: 5rem;
}
.digit:last-of-type {
  padding-right: 5rem;
}

:root {
  --lerp-0: 1; /* === sin(90deg) */
  --lerp-1: calc(sin(50deg));
  --lerp-2: calc(sin(45deg));
  --lerp-3: calc(sin(35deg));
  --lerp-4: calc(sin(25deg));
  --lerp-5: calc(sin(15deg));
}

.digit:is(:hover, :focus-visible) {
  --active: var(--lerp-0);
}
.digit:is(:hover, :focus-visible) + .digit,
.digit:has(+ .digit:is(:hover, :focus-visible)) {
  --active: var(--lerp-1);
}
.digit:is(:hover, :focus-visible) + .digit + .digit,
.digit:has(+ .digit + .digit:is(:hover, :focus-visible)) {
  --active: var(--lerp-2);
}
.digit:is(:hover, :focus-visible) + .digit + .digit + .digit,
.digit:has(+ .digit + .digit + .digit:is(:hover, :focus-visible)) {
  --active: var(--lerp-3);
}
.digit:is(:hover, :focus-visible) + .digit + .digit + .digit + .digit,
.digit:has(+ .digit + .digit + .digit + .digit:is(:hover, :focus-visible)) {
  --active: var(--lerp-4);
}
.digit:is(:hover, :focus-visible) + .digit + .digit + .digit + .digit + .digit,
.digit:has(+ .digit + .digit + .digit + .digit + .digit:is(:hover, :focus-visible)) {
  --active: var(--lerp-5);
}
```

The glide to reveal hover effect by Jhey features six digits that are blurred initially but scale up and become clear on hover or focus.

===
9. OS dock

CSS OS Dock [CSS :has(), lerp custom props]

```html
<h1>:hover</h1>
<nav class="blocks">
  <a href="#" class="block" style="--bg: var(--gradient-1);">
    <div class="block__item"></div>
  </a>
  <a href="#" class="block" style="--bg: var(--gradient-2);">
    <div class="block__item"></div>
  </a>
  <a href="#" class="block" style="--bg: var(--gradient-3);">
    <div class="block__item"></div>
  </a>
  <a href="#" class="block" style="--bg: var(--gradient-4);">
    <div class="block__item"></div>
  </a>
  <a href="#" class="block" style="--bg: var(--gradient-5);">
    <div class="block__item"></div>
  </a>
  <a href="#" class="block" style="--bg: var(--gradient-6);">
    <div class="block__item"></div>
  </a>
  <a href="#" class="block" style="--bg: var(--gradient-7);">
    <div class="block__item"></div>
  </a>
  <a href="#" class="block" style="--bg: var(--gradient-8);">
    <div class="block__item"></div>
  </a>
  <a href="#" class="block" style="--bg: var(--gradient-9);">
    <div class="block__item"></div>
  </a>
</nav>
```
```css
:root {
	--lerp-0: 1;
  --lerp-1: 0.5625;
  --lerp-2: 0.25;
  --lerp-3: 0.0625;
  --lerp-4: 0;
}

*,
*:after,
*:before {
	box-sizing: border-box;
}

h1 {
  position: fixed;
  bottom: var(--size-4);
  right: var(--size-4);
  margin: 0;
  color: var(--gray-0);
}

body {
	display: grid;
	place-items: center;
	min-height: 100vh;
	font-family:  'Google Sans', sans-serif, system-ui;
	background: var(--gradient-1);
}

:root {
  --lerp-0: 1;
  --lerp-1: 0.5625;
  --lerp-2: 0.25;
  --lerp-3: 0.0625;
  --lerp-4: 0;
}

.blocks {
	display: flex;
	list-style-type: none;
	padding: var(--size-2);
	border-radius: var(--radius-3);
	gap: var(--size-4);
	background: hsl(0 0% 100% / 0.5);
	align-items: center;
	justify-content: center;
	align-content: center;
	backdrop-filter: blur(10px);
}

.blocks:hover {
	--show: 1;
}

.block {
	width: 48px;
	height: 48px;
	display: grid;
	place-items: center;
	align-content: center;
	transition: flex 0.2s;
	flex: calc(0.2 + (var(--lerp, 0) * 1.5));
	position: relative;
}

.block:after {
	content: '';
	width: 5%;
	aspect-ratio: 1;
	background: var(--text-1);
	position: absolute;
	bottom: 10%;
	left: 50%;
	border-radius: var(--radius-3);
	transform: translate(-50%, -50%);
	z-index: -1;
}

.block:before {
	content: '';
	position: absolute;
	width: calc(100% + var(--size-4));
	bottom: 0;
	aspect-ratio: 1 / 2;
	left: 50%;
	transition: transform 0.2s;
	transform-origin: 50% 100%;
	transform: translateX(-50%) scaleY(var(--show, 0));
	z-index: -1;
}

.block .block__item {
  transition: outline 0.2s;
  outline: transparent var(--size-1) solid;
}

:is(.block:hover, .block:focus-visible) .block__item {
	outline: var(--surface-1) var(--size-1) solid;
}

.block {
	outline: none;
}

.block__item {
	width: 100%;
	aspect-ratio: 1;
	border-radius: var(--radius-3);
	background: var(--bg), var(--surface-1);
	display: inline-block;
	transition: transform 0.2s;
	transform-origin: 50% 100%;
	position: relative;
	transform: translateY(calc(var(--lerp) * -75%));
}

.block__item:after {
	content: '';
	position: absolute;
	height: 100%;
	top: 50%;
	left: 50%;
	transform: translate(-50%, -50%);
	aspect-ratio: 1;
	border-left: var(--size-2) solid white;
	border-top: var(--size-2) solid white;
	border-radius: var(--radius-3);
	mask: linear-gradient(135deg, black, transparent 50%);
}

@media(prefers-color-scheme: dark) {
	body {
		background: var(--gradient-23);
	}
	.blocks {
		background: hsl(0 0% 0% / 0.5);
	}
}


:is(.block:hover, .block:focus-visible) {
  --lerp: var(--lerp-0);
  z-index: 5;
}
.block:has( + :is(.block:hover, .block:focus-visible)),
:is(.block:hover, .block:focus-visible) + .block {
  --lerp: var(--lerp-1);
  z-index: 4;
}
.block:has( + .block + :is(.block:hover, .block:focus-visible)),
:is(.block:hover, .block:focus-visible) + .block + .block {
  --lerp: var(--lerp-2);
  z-index: 3;
}
.block:has( + .block + .block + :is(.block:hover, .block:focus-visible)),
:is(.block:hover, .block:focus-visible) + .block + .block + .block {
  --lerp: var(--lerp-3);
  z-index: 2;
}
.block:has( + .block + .block + .block + :is(.block:hover, .block:focus-visible)),
:is(.block:hover, .block:focus-visible) + .block + .block + .block + .block {
  --lerp: var(--lerp-4);
  z-index: 1;
}


body:has(a[style="--bg: var(--gradient-1);"]:focus) {
	background: var(--gradient-1);
}
body:has(a[style="--bg: var(--gradient-2);"]:focus) {
	background: var(--gradient-2);
}
body:has(a[style="--bg: var(--gradient-3);"]:focus) {
	background: var(--gradient-3);
}
body:has(a[style="--bg: var(--gradient-4);"]:focus) {
	background: var(--gradient-4);
}
body:has(a[style="--bg: var(--gradient-5);"]:focus) {
	background: var(--gradient-5);
}
body:has(a[style="--bg: var(--gradient-6);"]:focus) {
	background: var(--gradient-6);
}
body:has(a[style="--bg: var(--gradient-7);"]:focus) {
	background: var(--gradient-7);
}
body:has(a[style="--bg: var(--gradient-8);"]:focus) {
	background: var(--gradient-8);
}
body:has(a[style="--bg: var(--gradient-9);"]:focus) {
	background: var(--gradient-9);
}
```

Check out more CSS hover effects!
Explore our dedicated article to see more CSS hover effects.


===


Button animations

===

10. Keyframe tricks

Single Keyframe Tricks

The keyframe tricks effect by David East showcases various types of animations — like shake, pulse, glitch, flip, fill — applied to buttons using CSS keyframes. The JavaScript code in this Codepen is used to stop and start the animation.

```html
<main>
  <article>
    <section>
      <div>
        <h2>Shakes</h2>
        <p>Rejecti{ng</p>
      </div>
      <div>
        <button anim="shake">CLICK ME</button>
      </div>
    </section>
    <section>
      <div>
        <h2>Pulse</h2>
        <p>Partying</p>
      </div>
      <div>
        <button anim="pulse">CLICK ME</button>
      </div>
    </section>
    <section>
      <div>
        <h2>Glitch</h2>
        <p>Hacking</p>
      </div>
      <div>
        <button anim="glitch">CLICK ME</button>
      </div>
    </section>
    <section>
      <div>
        <h2>Turn</h2>
        <p>Flipping</p>
      </div>
      <div>
        <button anim="flip">CLICK ME</button>
      </div>
    </section>
    <section>
      <div>
        <h2>Fill</h2>
        <p>Loading</p>
      </div>
      <div>
        <button anim="fill">Click me</button>
      </div>
    </section>
    <section>
      <div>
        <h2>Sheen</h2>
        <p>Shining</p>
      </div>
      <div>
        <button anim="sheen">Click me</button>
      </div>
    </section>
    <section>
      <div>
        <h2>Emphasize</h2>
        <p>Glowing</p>
      </div>
      <div>
        <button anim="glow">Click me</button>
      </div>
    </section>
    <section>
      <div>
        <h2>Blur</h2>
        <p>Censoring</p>
      </div>
      <div>
        <button anim="blur">CLICK ME</button>
      </div>
    </section>
    <section>
      <div>
        <h2>Tony Hawk</h2>
        <p>900-ing</p>
      </div>
      <div>
        <button anim="tonyhawk">CLICK ME</button>
      </div>
    </section>
  </article>
  <article>
    <section>
      <div>
        <h2>Shakes</h2>
        <p>Rejecting</p>
      </div>
      <div>
        <button anim="shake">CLICK ME</button>
      </div>
    </section>
  </article>
  <article>
    <section>
      <div>
        <h2>Pulse</h2>
        <p>Partying</p>
      </div>
      <div>
        <button anim="pulse">CLICK ME</button>
      </div>
    </section>
  </article>
  <article>
    <section>
      <div>
        <h2>Glitch</h2>
        <p>Hacking</p>
      </div>
      <div>
        <button anim="glitch">CLICK ME</button>
      </div>
    </section>
  </article>
  <article>
    <section>
      <div>
        <h2>Turn</h2>
        <p>Flipping</p>
      </div>
      <div>
        <button anim="flip">CLICK ME</button>
      </div>
    </section>
  </article>
  <article>
    <section>
      <div>
        <h2>Fill</h2>
        <p>Loading</p>
      </div>
      <div>
        <button anim="fill">Click me</button>
      </div>
    </section>
  </article>
  <article>
    <section>
      <div>
        <h2>Sheen</h2>
        <p>Shining</p>
      </div>
      <div>
        <button anim="sheen">Click me</button>
      </div>
    </section>
  </article>
  <article>
    <section>
      <div>
        <h2>Emphasize</h2>
        <p>Glowing</p>
      </div>
      <div>
        <button anim="glow">Click me</button>
      </div>
    </section>
  </article>
  <article>
    <section>
      <div>
        <h2>Blur</h2>
        <p>Censoring</p>
      </div>
      <div>
        <button anim="blur">CLICK ME</button>
      </div>
    </section>
  </article>
  <article>
    <section>
      <div>
        <h2>Tony Hawk</h2>
        <p>900-ing</p>
      </div>
      <div>
        <button anim="tonyhawk">CLICK ME</button>
      </div>
    </section>
  </article>
</main>
```
```css
@import "https://unpkg.com/open-props@1.6.17/easings.min.css";

@keyframes shake {
  50% {
    transform: translate3d(20px, 0, 0);
  }
}

@keyframes flip {
  100% {
    transform: rotateY(180deg);
  }
}

@keyframes pulse {
  50% {
    transform: scale(1.5);
  }
}

@keyframes glitch {
  50% {
    transform: skew(180deg);
  }
}

@keyframes fill {
  50% {
    transform: translateX(-5%);
  }
}

@keyframes sheen {
  100% {
    transform: rotateZ(60deg) translate(1em, -9em);
  }
}

@keyframes glow {
  50% {
    box-shadow: 0 0 40px hsl(12, 100%, 60%);
  }
}

@keyframes tonyhawk {
  50%,
  100% {
    transform: rotate(900deg);
  }
}

@keyframes blur {
  50% {
    filter: blur(20px);
    transform: skew(45deg);
  }
}

[anim="shake"]:not(.toggled) {
  animation: shake var(--ease-elastic-in-1) 300ms infinite alternate;
}

[anim="pulse"]:not(.toggled) {
  animation: pulse var(--ease-elastic-in-1) 1400ms infinite alternate;
}

[anim="glitch"]:not(.toggled) {
  animation: glitch var(--ease-elastic-in-1) 600ms infinite;
}

[anim="tonyhawk"]:not(.toggled) {
  animation: tonyhawk var(--ease-elastic-in-1) 600ms infinite;
}

[anim="flip"]:not(.toggled) {
  animation: flip var(--ease-elastic-in-1) 600ms infinite alternate;
}

[anim="fill"]:not(.toggled)::after {
  animation: fill var(--ease-spring-1) 8000ms infinite;
}

[anim="sheen"]:not(.toggled)::after {
  animation: sheen var(--ease-elastic-in-1) 1s infinite;
}

[anim="glow"]:not(.toggled) {
  animation: glow var(--ease-elastic-in-1) 600ms infinite alternate;
}

[anim="blur"]:not(.toggled) {
  animation: blur var(--ease-elastic-in-1) 1s infinite alternate;
}

[anim="fill"]::after {
  content: "";
  color: var(--black);
  position: absolute;
  display: flex;
  justify-content: center;
  align-items: center;
  font-size: 0.75rem;
  height: 100%;
  width: 200%;
  inset: 0;
  transform: translateX(-400px);
  background-color: hsl(12, 90%, 63%);
}

[anim="sheen"]::after {
  content: "";
  position: absolute;
  top: -50%;
  right: -50%;
  bottom: -50%;
  left: -50%;
  background: var(--red-sheen);
  transform: rotateZ(60deg) translate(-5em, 7.5em);
}

:root {
  --black: hsl(0, 0%, 13%);
  --dark: hsl(12, 32%, 2%);
  --gray: hsl(0, 0%, 70%);
  --white: hsl(0, 0%, 96%);
  --red: hsl(12, 90%, 63%);
  --red-shadow: hsl(12, 100%, 60%);
  --red-sheen: linear-gradient(
    to bottom,
    hsl(12, 90%, 43%),
    hsla(12, 40%, 70%, 0.5) 50%,
    hsl(12, 93%, 23%)
  );
}

html,
body {
  background-color: var(--black);
}

div:has(h2 + p) {
  display: grid;
  gap: 8px;
}

h2 {
  font-size: 1.25rem;
}

h2 + p {
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--gray);
  font-size: 0.875rem;
}

main {
  max-width: 880px;
  margin: 0 auto;
  display: grid;
  grid-template-columns: 1fr;
  justify-content: center;
  align-items: center;
}

article {
  display: flex;
  flex-wrap: wrap;
  gap: 64px;
  align-items: center;
  justify-content: center;
  min-height: 100dvh;
  position: relative;
  padding-block: 32px;

  &::after {
    content: "";
    width: 100%;
    position: absolute;
    height: 1px;
    bottom: 0;
    background-image: linear-gradient(45deg, var(--red), transparent 70%);
  }
}

section {
  display: flex;
  flex-direction: column;
  gap: 16px;
  flex-basis: 180px;
  justify-content: center;
  align-items: center;
  text-align: center;
}

section {
  color: var(--white);
  font-family: "aglet-mono-variable", sans-serif;
  font-variation-settings: "wght" 400;
}

* {
  font-family: inherit;
  box-sizing: border-box;
}

button {
  all: unset;
  background-color: var(--black);
  padding: 6px 12px;
  border-radius: 6px;
  font-size: 0.875rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  border: 1px solid var(--red);
  box-shadow: 0 0 4px var(--red-shadow);
  cursor: pointer;
  perspective: 1000px;
  position: relative;
  overflow: hidden;
}

.blurry {
  position: relative;
  transform-style: preserve-3d;
}

.blurry::before {
  content: "";
  position: absolute;
  inset: 0px;
  transform: translate3d(0px, 0px, -1px);
  background: var(--red-shadow);
  filter: blur(6px);
}

```
```javascript
let anims = [...document.querySelectorAll("[anim]")];
console.log(anims);
let click = (el, cb) => el.addEventListener("click", cb);
let toggle = (el) => el.classList.toggle("toggled");
let clickTog = (el) => click(el, () => toggle(el));
anims.map(clickTog);

```



===


11. CSS fizzy button

CSS Fizzy Button

The CSS fizzy button by Jamie Coulter creates an animated button effect where bubble particles emanate from the text button while a right arrow slides into view and the background color changes.

```haml
.button
    %h1 Fizzy CSS Button
    %h2 With super fizzy particle action
    %a{:href => 'https://www.codepen.io/jcoulterdesign'}
        %i.ion-social-codepen
        %span More Codepen shenanigans
    %input#button{:type => 'checkbox'}
    %label{:for => 'button'}
        .button_inner.q
            %i.l.ion-log-in
            %span.t Downloads
            %span 
                %i.tick.ion-checkmark-round
            .b_l_quad
                -(1..52).each do
                    .button_spots
    
    
```
```scss
@import url(https://fonts.googleapis.com/css?family=Fira+Sans:400,300,700,500,400italic,500italic,700italic,300italic);

body{
    background: #2C3940;
    overflow:hidden;
    font-family: 'Fira Sans', sans-serif;
    a{
        color:white;
        padding-top:20px;
        font-size:10px;
        opacity:0.6;
        position:relative;
        top:10px;
        transition:all .3s;
        font-weight:100;
        text-decoration:none;
        &:hover{
            opacity:1;

        }
        i.l{
            margin-right:3px;
        }
    }
}
input{
    display:none;
}

input:checked + label{
    .button_inner{
        background:transparent;
        transform:rotate(90deg);
        width:100px;
        border-radius:100px;
        box-shadow:0px 0px 0px 440px rgba(0,0,0,0);
        animation: finalbox .4s 4.42s cubic-bezier(0.39, 2.01, 0.27, 0.75) forwards;
        span.t{
            opacity:0;
            top:20px;
        }
    }
    i.l{
        left:14px;
        opacity:1;
        top:11px;
        animation:down 1s .25s infinite,final .2s 4s forwards;
    }
    .tick{
        position: absolute;
        left: 2px;
        right: 0;
        transform:scale(0) rotate(-90deg);
        color: #00C1FC;
        top: 11px;
        margin: auto;
        font-size: 22px;
        animation: tick .3s 4.7s forwards;
    }
    .button_spots{
        opacity:1;
        @for $i from 0 through 60{
            &:nth-of-type(#{$i}){
                top:(16px - random(10)) - 0 !important;
                left:-34px  !important;
                opacity:0;
                padding:random(20)/4 + 2 + px !important;
                animation:spew 1s .3s forwards,rotate 4s + random(4)/10 .25s + random(12)/10 linear infinite ,final .2s 4s forwards,spot-#{$i} .7s .4/random(10) + random(10)/10 + 10s linear infinite !important;
            }
        }

    }
}
.tick{
    position:absolute;
    left:-40;
    right:0;
    transform:scale(0);
    margin:auto;
    font-size:22px;
}
.button{
    position:absolute;
    top:50%;
    left:0;
    right:0;
    margin:auto;
    text-align:Center;
    height:360px;
    width:200px;
    transform:translateY(-50%);
    h1{
        font-weight:100;
        color:White;
        font-size:24px;
        margin:0;
        text-transform:uppercase;
    }
    h2{
        font-weight:100;
        color:#00C4FF;
        opacity:1;
        font-size:14px;
        margin:4px 0px 0px 0px;
    }
    .b_l_quad .button_spots{
        @for $i from 1 through 20{
            &:nth-child(#{$i}){
                padding:random(3) + 2 + px;
                left: -25 + ($i * 12) + px;
                top: 50 + px;
            }
        }
        @for $i from 20 through 40{
            &:nth-child(#{$i}){
                padding:random(3) + 2 + px;
                left: -255 + ($i * 12) + px;
                top: -12 + px;
            }
        }
        @for $i from 40 through 46{
            &:nth-child(#{$i}){
                padding:random(3) + 2 + px;
                left: 204px;
                top: -488 + ($i * 12) + px;
            }
        }

        @for $i from 46 through 52{
            &:nth-child(#{$i}){
                padding:random(3) + 2 + px;
                left: -10px;
                top: -568 + ($i * 12) + px;
            }
        }
    }
    .button_spots{
        position:absolute;
        border-radius:100px;
        background:green;
        opacity:0;
        animation:opacity 1s;
        @for $i from 1 through 52{
            &:nth-of-type(#{$i}){
                transform-origin:90px - random(10) 20px - random(10);
                background:hsla(350 + random(399) ,57% - random(10) ,65%,1);
                box-shadow:0px 0px 10px rgba(255,255,255,0.12);
                transition:all 1s + random(10)/10;
            }
        }
    }
    &_inner{
        //overflow:hidden;
        border-radius:2px;
        position:absolute;
        width:200px;
        height:50px;
        left:0;
        right:0;
        top:50%;
        margin:auto;
        box-shadow: 0px 0px 0px 0px rgba(0, 0, 0, 0.04);
        font-weight: 100;
        font-size: 12px;
        cursor:pointer;
        border: 2px solid #FFFFFF;
        color: white;
        text-align: Center;
        transition:all .3s,box-shadow .2s,transform .2s .2s;
        span.t{
            position:relative;
            top:6px;
            opacity:1;
            left:-10px;
            transition:left .4s .1s;


        }
        i.l{
            position: relative;
            left: -19px;
            top: 20px;
            color: #00C4FF;
            font-size: 25px;
            opacity: 0;
            transition: left .3s 0s,top .3s 0s,opacity .3s 0s;
        }
        &:hover{
            color:#2C3940;
            background: white;
            box-shadow: 0px 17px 18px -14px rgba(0, 0, 0, 0.08);
            span.t{
                left:16px;
                transition:left .4s;
            }
            i.l{
                top:12px;
                opacity:1;
                transition: left .3s 0s,top .3s .1s,opacity .3s .1s;
            }
        }
        &:hover .button_spots{
            @for $i from 1 through 19{
                &:nth-of-type(#{$i}){
                    animation: spot-#{$i} .7s .4/random(10) + random(10)/10 + s linear infinite;
                }
            }
            @for $i from 20 through 40{
                &:nth-of-type(#{$i}){
                    animation: spot-#{$i} .7s .4/random(10) + random(10)/10 + s linear infinite;
                }
            }
            @for $i from 40 through 46{
                &:nth-of-type(#{$i}){
                    animation: spot-#{$i} .7s .4/random(10) + random(10)/10 + s linear infinite;
                }
            }
            @for $i from 46 through 54{
                &:nth-of-type(#{$i}){
                    animation: spot-#{$i} .7s .4/random(10) + random(10)/10 + s linear infinite;
                }
            }
        }
    }
}

@for $i from 1 through 20{
    @keyframes spot-#{$i}{
        from{opacity:0;}
        to{transform:translateY(30px) translatex(-20px + $i*2);opacity:.6;}
    }
}
@for $i from 20 through 40{
    @keyframes spot-#{$i}{
        from{opacity:0;}
        to{transform:translateY(-30px) translatex(-50px + $i*2);opacity:.6;}
    }
}
@for $i from 40 through 45{
    @keyframes spot-#{$i}{
        from{opacity:0;}
        to{transform:translateY(-86px + $i*2) translatex(40px);opacity:.6;}
    }
}
@for $i from 46 through 54{
    @keyframes spot-#{$i}{
        from{opacity:0;}
        to{transform:translateY(-99px + $i*2) translatex(-40px);opacity:.6;}
    }
}

@keyframes opacity{
    from{}
    to{opacity:0;}
}

@keyframes rotate{
    from{opacity:.8}
    to{transform:rotate(360deg);opacity:.8}
}

@keyframes down{
    from{left:10px;}
    to{left:57px;}
}

@keyframes spew{
    from{opacity:0;}
    to{opacity:0.8;}
}

@keyframes final{
    from{opacity:1;}
    to{opacity:0;}
}
@keyframes finalbox{
    from{}
    to{width:50px;}
}
@keyframes tick{
    from{}
    to{transform:scale(1) rotate(-90deg)}
}
```

===


12. Fun 3D button

Fun 3D button

The fun 3D button by LukyVJ creates a button that, when hovered over, scales up while heart-shaped elements of different sizes scale into view. It also provides controls for debugging the animation and changing the body’s background color.

Take your 3D animations to the next level with GSAP and Three.js!
Learn to animate 3D models with GSAP and Three.js in our interactive course! In this creative course, we guide you through building a scroll-animated 3D landing page using Next.js 14, GSAP, Prismic, Three.js, Tailwind, and TypeScript.

```html
<main>
  <button>
    <span></span>
    <span>Play</span>
  </button>
</main>

<div 
     id="info-box" data-info-chrome="🟠 In Chrome: The first hover can look clunky, no idea why" data-info-safari="✅ In Safari" data-info-firefox="✅ In  Firefox" 
     data-presentation-width="60vw"
     data-presentation-height="60vh"
     data-inspiration="https://twitter.com/aleksliving/status/1635306125393551360"></div>
```
```scss
// The following would be the same font as the inspiration tweet,
// but I like Mona Sans better for now :D 
// I still keep the gfont url just in case
@import url('https://fonts.googleapis.com/css2?family=Montserrat+Alternates:wght@500&display=swap');

@font-face {
  font-family: "Mona Sans";
  src: url("https://assets.codepen.io/64/Mona-Sans.woff2")
      format("woff2 supports variations"),
    url("https://assets.codepen.io/64/Mona-Sans.woff2")
      format("woff2-variations");
  font-weight: 100 1000;
}

:root {
  --icon-scale: 0;
  --icon-rotation: 0;
  --icon-opacity: 0;

  --color-pale-pink: hsl(272 16% 56.99%);
  --color-dark-purple: hsl(269.35 100% 21%);
  --color-light-purple: hsl(269.99 100% 40%);
  --color-bubblegum-pink: hsl(300deg 97% 68%);
}

@supports (color: color(display-p3 0 0 0)) {
  :root {
    --color-pale-pink: color(display-p3 0.56 0.5 0.63);
    --color-dark-purple: color(display-p3 0.19 0.01 0.41);
    --color-light-purple: color(display-p3 0.37 0.01 0.78);
    --color-bubblegum-pink: color(display-p3 0.94 0.36 1);
  }
}

*,
*:before,
*:after {
  box-sizing: border-box;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  outline: calc(var(--debug) * 1px) dotted
    hsl(calc(var(--debug) * 10 * 1deg) 60% 60%);
}

html,
body,
main {
  width: 100%;
  height: 100%;
  margin: 0;
  padding: 0;
}

body {
  background: hsl(271deg 100% 96%);
  font-family: "Mona Sans", sans-serif;
}

main {
  display: grid;
  place-items: center;
  
  --color-mix: color-mix(
        in lch,
        var(--color-bubblegum-pink) 60%,
        var(--color-dark-purple)
      );

  & button {
    all: unset;
    cursor: pointer;
    font-size: 3em;
    font-weight: 1000;
    color: white;
    position: relative;
    transition: transform .2s ease;

    &:before {
      content: "";
      display: block;
      position: absolute;
      width: 100%;
      height: 100%;
      
       background: linear-gradient(
          to bottom right,
          var(--color-pale-pink),
          var(--color-pale-pink)
        );
      transform: scale(0.9);
      outline: 0px solid var(--color-light-purple);
      border-radius: 1e5px;
      transition: transform 0.2s ease, background 0.2s ease, box-shadow 0.2s ease;
    }

    span:nth-child(2) {
      position: relative;
      padding: 0.5em 1.2em;
      width: 100%;
      height: 100%;
      top: 0;
      left: 0;
      z-index: 2;
      display: block;
      border-radius: 1e5px;
      transition: transform 0.2s ease;

      &:before,
      &:after {
        content: "";
        display: block;
        position: absolute;
        width: 100%;
        height: 100%;
        top: 0;
        left: 0;
        opacity: var(--icon-opacity);
        transform: scale(var(--icon-scale))
          rotate(calc(var(--icon-rotation) * -1deg));
        transition: transform 0.2s ease, opacity .2s ease,
          --icon-rotation 0.4s cubic-bezier(.02,1.02,.67,1.06);
        pointer-events: none;
        z-index: -1;
      }

      &:before {
        width: 2.1em;
        top: -0.8em;
        left: -0.8em;
        background: url(https://assets.codepen.io/64/heart+%286%29+%281%29.png)
          no-repeat center center / contain;
        z-index: -1;
         filter:  drop-shadow(0 2px 4px rgb(0 0 0 / 20%)) saturate(150%);
      }

      &:after {
        width: 1.2em;
        top: 1em;
        left: 0.5em;
        background: url(https://assets.codepen.io/64/heart+%285%29+%281%29.png)
          no-repeat center center / contain;
        filter: blur(2px) drop-shadow(0 2px 4px rgb(0 0 0 / 20%)) saturate(150%);
      }
    }
    
   
    span:nth-child(1) {
      position: absolute;
      width: 100%;
      height: 100%;
      top: 0;
      left: 0;
      background: transparent;

      &:before,
      &:after {
        content: "";
        display: block;
        position: absolute;
        width: 100%;
        height: 100%;
        top: 0;
        right: 0;
        z-index: 5;
        transform: scale(var(--icon-scale))
          rotate(calc(var(--icon-rotation) * -1deg));
        transition: transform 0.2s ease, opacity .2s ease,
          --icon-rotation 0.4s cubic-bezier(.02,1.02,.67,1.06);
        pointer-events: none;
       
      }

      &:before {
        width: 1.75em;
        top: -1em;
        right: 0.5em;
        background: url(https://assets.codepen.io/64/heart+%283%29+%281%29.png)
          no-repeat center center / contain;
        filter: blur(1px) drop-shadow(0 2px 4px rgb(0 0 0 / 20%)) saturate(150%);
      }

      &:after {
        width: 2em;
        top: 1em;
        right: -0.5em;
        background: url(https://assets.codepen.io/64/heart+%284%29+%281%29.png)
          no-repeat center center / contain;
         filter:  drop-shadow(0 2px 4px rgb(0 0 0 / 20%)) saturate(150%);
      }
    }

    &:hover {
      --icon-scale: 1;
      --icon-rotation: 0;
      --icon-opacity: 1;
      

      &:before {
        transform: scale(1.05);
        background: linear-gradient(
          to bottom right,
          var(--color-dark-purple),
          var(--color-light-purple)
        );
        box-shadow: 0 4px 22px -8px var(--color-mix);
      }

      & > span:nth-child(2) {
        transform: scale(0.95);
      }
      
      &:active {
        --icon-scale: 0.8;
        --icon-rotation: 20;

        transform: scale(1.05);

        &:before {
          box-shadow: 0 2px 12px var(--color-mix);
        }

        span:before,
        span:after {
          transition-delay: 0s;
        }

        span:nth-child(odd):before,
        span:nth-child(even):before {
          --icon-rotation: -20;
        }

        span:nth-child(odd):after,
        span:nth-child(even):after {
          --icon-rotation: 20;
        }

        &:before {
          transform: scale(0.95);
        }
      }
    }
  }
}

```


===

13. Rainbow gradient button

Lil rainbow gradient button

The rainbow gradient button by LukyVJ creates a button that scales up, shifts, and shuffles through various colors on hover. It relies on several colors, CSS variables, scale, translateY, and linear-gradient (docs) to work.

```html
<main>
  <button>
    <div>
      <span>Join today!</span>
    </div>
  </button>
</main>


<!--info box-->
<div class="info-box" 
     data-info=""
     data-issue=""
>
  <p>Made by <a href="https://twitter.com/lukyvj" target="_blank">@LukyVj</a></p>
  
</div>

```
```scss
@font-face {
  font-family: "Mona Sans";
  src: url("https://assets.codepen.io/64/Mona-Sans.woff2")
      format("woff2 supports variations"),
    url("https://assets.codepen.io/64/Mona-Sans.woff2")
      format("woff2-variations");
  font-weight: 100 1000;
}

@layer properties {
  @property --bg-position {
    syntax: "<number>";
    inherits: true;
    initial-value: 100;
  }
  @property --after-blur {
    syntax: "<number>";
    inherits: true;
    initial-value: 0;
  }
  @property --after-opacity {
    syntax: "<number>";
    inherits: true;
    initial-value: 1;
  }
  @property --before-opacity {
    syntax: "<number>";
    inherits: true;
    initial-value: 0.3;
  }
  @property --btn-offset {
    syntax: "<number>";
    inherits: true;
    initial-value: 1;
  }
  @property --btn-scale {
    syntax: "<number>";
    inherits: true;
    initial-value: 1;
  }
}

:root {
  --debug: 0;

  /* colors */
  --body-bg: hsl(0, 0%, 6%);
  --btn-bg: hsl(0, 0%, 0%);
  --btn-border-width: 1.5;
  --btn-offset: 1;
  --btn-scale: 1;
  --after-bg: linear-gradient(
    to right,
    rgb(0 0 0),
    rgb(0 0 0),
    rgb(0 0 0),
    rgb(0 0 0),
    rgb(0 0 0),
    rgb(0 0 0),
    rgb(0 0 0),
    rgb(0 0 0),
    rgb(0 0 0),
    rgb(0 0 0),
    rgb(0 0 0),
    rgb(0 0 0),
    rgb(0 0 0)
  );
  --after-blur: 10;
  --after-opacity: 1;
  --after-pos-y: 10;
  
  --before-opacity: 0.3;

  /* positions */
  --bg-position: 100;

  --color-white: hsl(0, 0%, 100%);
  --color-cyan: hsl(180, 100%, 50%);
  --color-blue: hsl(240, 100%, 50%);
  --color-purple: hsl(270, 100%, 50%);
  --color-pink: hsl(330, 40%, 70%);
  --color-red: hsl(0, 100%, 50%);
  --color-yellow: hsl(60, 100%, 50%);
  --color-lime: hsl(90, 100%, 75%);

  --color-orange: oklch(69.1% 0.223 36.85);
}

@supports (color: color(display-p3 0 0 0)) {
  :root {
    --color-white: color(display-p3 1 1 1);
    --color-cyan: color(display-p3 0 1 1);
    --color-blue: color(display-p3 0 0 1);
    --color-purple: color(display-p3 0.5 0 1);
    --color-pink: color(display-p3 1 0.4 0.7);
    --color-red: color(display-p3 1 0 0);
    --color-yellow: color(display-p3 1 1 0);
    --color-lime: color(display-p3 0.75 1 0);

    --color-orange: color(display-p3 0.96 0.39 0.2);
  }
}

*,
*:before,
*:after {
  box-sizing: border-box;
  outline: calc(var(--debug) * 1px) dotted red;
  outline-offset: -1px;
}

html,
body,
main {
  width: 100%;
  height: 100%;
  padding: 0;
  margin: 0;
}

body {
  background: var(--body-bg);
  font-family: "Mona Sans", sans-serif;
  padding: 0 6em;
}

main {
  display: grid;
  place-items: center;

  & button {
    all: unset;
    background: transparent;
    border-width: 0;
    transform: scale(var(--btn-scale));
    transition: 
        --bg-position 3s ease, 
        --after-blur 0.3s ease,
        --before-opacity 0.3s ease,
        --btn-offset 0.3s ease,
        --btn-scale 0.2s cubic-bezier(.76,-0.25,.51,1.13);

    > div {
      display: block;
      padding: 0.8em 1.2em;
      background: var(--btn-bg);
      color: white;
      font-weight: bold;
      border-radius: 8px;
      font-size: 22px;
      position: relative;
      
      cursor: pointer;

      &:not(:hover) {
        transition: --after-blur 0.3s ease;
      }

      > span {
        background: linear-gradient(
            to right,
            var(--color-white),
            var(--color-white),
            var(--color-cyan),
            var(--color-blue),
            var(--color-purple),
            var(--color-pink),
            var(--color-red),
            var(--color-yellow),
            var(--color-lime),
            var(--color-white),
            var(--color-white)
          )
          no-repeat calc(var(--bg-position) * 1%) 0% / 900%;
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        letter-spacing: 0.15ch;
        font-weight: 600;
      }

      &:after {
        display: block;
        position: absolute;
        content: "";
        width: 100%;
        height: 100%;
        background: var(--after-bg) no-repeat calc(var(--bg-position) * 1%) 0% /
          900%;
        transform: translateY(calc(var(--after-pos-y) * 1px));
        left: 0;
        top: 0;
        z-index: -2;
        filter: blur(calc(var(--after-blur) * 1px));
        opacity: var(--after-opacity);
      }

      &:before {
        content: "";
        display: block;
        position: absolute;
        width: calc(100% + calc(calc(var(--btn-border-width) * 2) * 1px));
        height: calc(100% + calc(calc(var(--btn-border-width) * 2) * 1px));
        background: linear-gradient(
            to right,
            var(--color-white),
            var(--color-white),
            var(--color-cyan),
            var(--color-blue),
            var(--color-purple),
            var(--color-pink),
            var(--color-red),
            var(--color-yellow),
            var(--color-lime),
            var(--color-white),
            var(--color-white)
          )
          no-repeat calc(var(--bg-position) * 1%) 0% / 900%;
        border-radius: 9px;
        z-index: -1;
        top: calc(var(--btn-border-width) * -1px);
        left: calc(var(--btn-border-width) * -1px);
        opacity: var(--before-opacity);
      }
    }

    &:hover {
      --btn-scale: 1.05;
      --bg-position: 0;
      --after-bg: linear-gradient(
          to right,
          var(--color-white),
          var(--color-white),
          var(--color-cyan),
          var(--color-blue),
          var(--color-purple),
          var(--color-pink),
          var(--color-red),
          var(--color-yellow),
          var(--color-lime),
          var(--color-white),
          var(--color-white)
        );
        --after-blur: 30;
        --after-opacity: 0.3;
        --after-pos-y: 0;
        --before-opacity: 1;      
        --btn-offset: 5;
      
      &:active {
        --btn-scale: 0.98;
        --after-blur: 15;
      }
    }
  }
}

```
===


14. Storm button

Andreas Storm button

The storm button by jwktje uses HTML, CSS, and SVG to create a voltage button with a glowing effect. The SVG includes a filter named “glow” that gives the graphic a glowing, wavy look. The effect relies on three keyframes to work: spark-1, spark-2, and fly-up.

```html
<!-- https://twitter.com/avstorm/status/1620087443927228417 -->
<div class="voltage-button">
  
  <button>Button</button>
  
  <!-- SVG Party -->
  <svg version="1.1" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" x="0px" y="0px" viewBox="0 0 234.6 61.3" preserveAspectRatio="none" xml:space="preserve">
    <filter id="glow">
      <fegaussianblur class="blur" result="coloredBlur" stddeviation="2"></fegaussianblur>
      <feTurbulence type="fractalNoise" baseFrequency="0.075" numOctaves="0.3" result="turbulence"></feTurbulence>
  <feDisplacementMap in="SourceGraphic" in2="turbulence" scale="30" xChannelSelector="R" yChannelSelector="G" result="displace"></feDisplacementMap>
      <femerge>
        <femergenode in="coloredBlur"></femergenode>
        <femergenode in="coloredBlur"></femergenode>
        <femergenode in="coloredBlur"></femergenode>
        <femergenode in="displace"></femergenode>
        <femergenode in="SourceGraphic"></femergenode>
      </femerge>
    </filter>
    <path class="voltage line-1" d="m216.3 51.2c-3.7 0-3.7-1.1-7.3-1.1-3.7 0-3.7 6.8-7.3 6.8-3.7 0-3.7-4.6-7.3-4.6-3.7 0-3.7 3.6-7.3 3.6-3.7 0-3.7-0.9-7.3-0.9-3.7 0-3.7-2.7-7.3-2.7-3.7 0-3.7 7.8-7.3 7.8-3.7 0-3.7-4.9-7.3-4.9-3.7 0-3.7-7.8-7.3-7.8-3.7 0-3.7-1.1-7.3-1.1-3.7 0-3.7 3.1-7.3 3.1-3.7 0-3.7 10.9-7.3 10.9-3.7 0-3.7-12.5-7.3-12.5-3.7 0-3.7 4.6-7.3 4.6-3.7 0-3.7 4.5-7.3 4.5-3.7 0-3.7 3.6-7.3 3.6-3.7 0-3.7-10-7.3-10-3.7 0-3.7-0.4-7.3-0.4-3.7 0-3.7 2.3-7.3 2.3-3.7 0-3.7 7.1-7.3 7.1-3.7 0-3.7-11.2-7.3-11.2-3.7 0-3.7 3.5-7.3 3.5-3.7 0-3.7 3.6-7.3 3.6-3.7 0-3.7-2.9-7.3-2.9-3.7 0-3.7 8.4-7.3 8.4-3.7 0-3.7-14.6-7.3-14.6-3.7 0-3.7 5.8-7.3 5.8-2.2 0-3.8-0.4-5.5-1.5-1.8-1.1-1.8-2.9-2.9-4.8-1-1.8 1.9-2.7 1.9-4.8 0-3.4-2.1-3.4-2.1-6.8s-9.9-3.4-9.9-6.8 8-3.4 8-6.8c0-2.2 2.1-2.4 3.1-4.2 1.1-1.8 0.2-3.9 2-5 1.8-1 3.1-7.9 5.3-7.9 3.7 0 3.7 0.9 7.3 0.9 3.7 0 3.7 6.7 7.3 6.7 3.7 0 3.7-1.8 7.3-1.8 3.7 0 3.7-0.6 7.3-0.6 3.7 0 3.7-7.8 7.3-7.8h7.3c3.7 0 3.7 4.7 7.3 4.7 3.7 0 3.7-1.1 7.3-1.1 3.7 0 3.7 11.6 7.3 11.6 3.7 0 3.7-2.6 7.3-2.6 3.7 0 3.7-12.9 7.3-12.9 3.7 0 3.7 10.9 7.3 10.9 3.7 0 3.7 1.3 7.3 1.3 3.7 0 3.7-8.7 7.3-8.7 3.7 0 3.7 11.5 7.3 11.5 3.7 0 3.7-1.4 7.3-1.4 3.7 0 3.7-2.6 7.3-2.6 3.7 0 3.7-5.8 7.3-5.8 3.7 0 3.7-1.3 7.3-1.3 3.7 0 3.7 6.6 7.3 6.6s3.7-9.3 7.3-9.3c3.7 0 3.7 0.2 7.3 0.2 3.7 0 3.7 8.5 7.3 8.5 3.7 0 3.7 0.2 7.3 0.2 3.7 0 3.7-1.5 7.3-1.5 3.7 0 3.7 1.6 7.3 1.6s3.7-5.1 7.3-5.1c2.2 0 0.6 9.6 2.4 10.7s4.1-2 5.1-0.1c1 1.8 10.3 2.2 10.3 4.3 0 3.4-10.7 3.4-10.7 6.8s1.2 3.4 1.2 6.8 1.9 3.4 1.9 6.8c0 2.2 7.2 7.7 6.2 9.5-1.1 1.8-12.3-6.5-14.1-5.5-1.7 0.9-0.1 6.2-2.2 6.2z" fill="transparent" stroke="#fff"/>
    <path class="voltage line-2" d="m216.3 52.1c-3 0-3-0.5-6-0.5s-3 3-6 3-3-2-6-2-3 1.6-6 1.6-3-0.4-6-0.4-3-1.2-6-1.2-3 3.4-6 3.4-3-2.2-6-2.2-3-3.4-6-3.4-3-0.5-6-0.5-3 1.4-6 1.4-3 4.8-6 4.8-3-5.5-6-5.5-3 2-6 2-3 2-6 2-3 1.6-6 1.6-3-4.4-6-4.4-3-0.2-6-0.2-3 1-6 1-3 3.1-6 3.1-3-4.9-6-4.9-3 1.5-6 1.5-3 1.6-6 1.6-3-1.3-6-1.3-3 3.7-6 3.7-3-6.4-6-6.4-3 2.5-6 2.5h-6c-3 0-3-0.6-6-0.6s-3-1.4-6-1.4-3 0.9-6 0.9-3 4.3-6 4.3-3-3.5-6-3.5c-2.2 0-3.4-1.3-5.2-2.3-1.8-1.1-3.6-1.5-4.6-3.3s-4.4-3.5-4.4-5.7c0-3.4 0.4-3.4 0.4-6.8s2.9-3.4 2.9-6.8-0.8-3.4-0.8-6.8c0-2.2 0.3-4.2 1.3-5.9 1.1-1.8 0.8-6.2 2.6-7.3 1.8-1 5.5-2 7.7-2 3 0 3 2 6 2s3-0.5 6-0.5 3 5.1 6 5.1 3-1.1 6-1.1 3-5.6 6-5.6 3 4.8 6 4.8 3 0.6 6 0.6 3-3.8 6-3.8 3 5.1 6 5.1 3-0.6 6-0.6 3-1.2 6-1.2 3-2.6 6-2.6 3-0.6 6-0.6 3 2.9 6 2.9 3-4.1 6-4.1 3 0.1 6 0.1 3 3.7 6 3.7 3 0.1 6 0.1 3-0.6 6-0.6 3 0.7 6 0.7 3-2.2 6-2.2 3 4.4 6 4.4 3-1.7 6-1.7 3-4 6-4 3 4.7 6 4.7 3-0.5 6-0.5 3-0.8 6-0.8 3-3.8 6-3.8 3 6.3 6 6.3 3-4.8 6-4.8 3 1.9 6 1.9 3-1.9 6-1.9 3 1.3 6 1.3c2.2 0 5-0.5 6.7 0.5 1.8 1.1 2.4 4 3.5 5.8 1 1.8 0.3 3.7 0.3 5.9 0 3.4 3.4 3.4 3.4 6.8s-3.3 3.4-3.3 6.8 4 3.4 4 6.8c0 2.2-6 2.7-7 4.4-1.1 1.8 1.1 6.7-0.7 7.7-1.6 0.8-4.7-1.1-6.8-1.1z" fill="transparent" stroke="#fff"/>
  </svg>
  <div class="dots">
    <div class="dot dot-1"></div>
    <div class="dot dot-2"></div>
    <div class="dot dot-3"></div>
    <div class="dot dot-4"></div>
    <div class="dot dot-5"></div>
  </div>
</div>
```
```scss
body {
  background:black;
  display:flex;
  justify-content:center;
  align-items:center;
  height:100vh;
}
.voltage-button {
  position:relative;
  button {
    color:white;
    background: #0D1127;
    padding:1.6rem 5rem 1.8rem 5rem;
    border-radius:5rem;
    border:5px solid #5978F3;
    font-size:1.8rem;
    line-height:1em;
    letter-spacing:0.075em;
    transition:background 0.3s;
    &:hover {
      cursor:pointer;
      background: #0F1C53;
      + svg, +svg + .dots {
        opacity:1;
      }
    }
  }
  svg {
    display:block;
    position:absolute;
    top:-0.75em;
    left:-0.25em;
    width:calc(100% + 0.5em);
    height:calc(100% + 1.5em);
    pointer-events:none;
    opacity:0;
    transition:opacity 0.4s;
    transition-delay:0.1s;
    path {
      stroke-dasharray: 100;
      filter: url('#glow');
      &.line-1 {
        stroke:#f6de8d;
        stroke-dashoffset: 0;
        animation: spark-1 3s linear infinite;
      }
      &.line-2 {
        stroke:#6bfeff;
        stroke-dashoffset: 500;
        animation: spark-2 3s linear infinite;
      }
    }
  }
  .dots {
    opacity:0;
    transition:opacity 0.3s;
    transition-delay:0.4s;
    .dot {
      width:1rem;
      height:1rem;
      background:white;
      border-radius:100%;
      position:absolute;
      opacity:0;
    }
    .dot-1 {
      top:0;
      left:20%;
      animation: fly-up 3s linear infinite;
    }
    .dot-2 {
      top:0;
      left:55%;
      animation: fly-up 3s linear infinite;
      animation-delay: 0.5s;
    }
    .dot-3 {
      top:0;
      left:80%;
      animation: fly-up 3s linear infinite;
      animation-delay: 1s;
    }
    .dot-4 {
      bottom:0;
      left:30%;
      animation: fly-down 3s linear infinite;
      animation-delay: 2.5s;
    }
    .dot-5 {
      bottom:0;
      left:65%;
      animation: fly-down 3s linear infinite;
      animation-delay: 1.5s;
    }
  }
}

@keyframes spark-1 {
  to {
    stroke-dashoffset: -1000;
  }
}
@keyframes spark-2 {
  to {
    stroke-dashoffset: -500;
  }
}
@keyframes fly-up{
  0% { opacity:0; transform:translateY(0) scale(0.2); }
  5% { opacity:1; transform:translateY(-1.5rem) scale(0.4);}
  10%,100%{ opacity:0;  transform:translateY(-3rem) scale(0.2);}
}
@keyframes fly-down {
  0% { opacity:0; transform:translateY(0) scale(0.2); }
  5% { opacity:1; transform:translateY(1.5rem) scale(0.4);}
  10%,100%{ opacity:0;  transform:translateY(3rem) scale(0.2);}
}
```


===
# Click animations


===


15. Toggle switch with a hole

Toggle Switch with a Hole Handle


Jon Kantner created a toggle switch with a hole that, when clicked, the hole extends and moves to the opposite end along the x-axis. It’s a solid implementation of the basic toggle switch animation we see around.

```html
<input class="pristine" type="checkbox" name="toggle" value="on">
```
```css
* {
	border: 0;
	box-sizing: border-box;
	margin: 0;
	padding: 0;
}
body, input {
	font: 80px/1.5 sans-serif;
}
body, input[type=checkbox]:before {
	background-image:
		linear-gradient(90deg,#f1f2f3 2px,#f1f2f300 2px),
		linear-gradient(#f1f2f3 2px,#fff 2px);
	background-repeat: repeat;
	background-size: 0.75em 0.375em;
}
body {
	background-position: 50% calc(50% + 0.2em);
	display: grid;
	place-items: center;
	height: 100vh;
}
input[type=checkbox] {
	--off: #c7cad1;
	--mid: #829ad6;
	--on: #255ff4;
	--transDur: 0.5s;
	--timing: cubic-bezier(0.6,0,0.4,1);
	animation: bgOff var(--transDur) var(--timing);
	background-color: var(--off);
	border-radius: 0.67em / 0.5em;
	box-shadow:
		0 0.05em 0.1em #00000007 inset,
		0 -0.25em 0.25em #0001 inset,
		0 -0.5em 0 #0001 inset,
		0 0.1em 0.1em #0001;
	cursor: pointer;
	position: relative;
	width: 2.25em;
	height: 1.5em;
	-webkit-appearance: none;
	appearance: none;
	-webkit-tap-highlight-color: transparent;
}
input[type=checkbox]:before {
	animation: handleOff var(--transDur) var(--timing);
	background-attachment: fixed;
	background-position: 50% calc(50% - 0.1875em);
	border-radius: 0.5em / 0.375em;
	box-shadow:
		0 0.175em 0.175em 0 #0001 inset,
		0 0.375em 0 #0002 inset,
		0 0.375em 0 var(--off) inset,
		0 0.475em 0.1em #0001 inset;
	content: "";
	display: block;
	position: absolute;
	top: 0.125em;
	left: 0.125em;
	width: 1em;
	height: 0.75em;
}
input[type=checkbox]:checked {
	animation: bgOn var(--transDur) var(--timing) forwards;
}
input[type=checkbox]:checked:before {
	animation: handleOn var(--transDur) var(--timing) forwards;
}
input[type=checkbox]:focus {
	outline: none;
}
input[type=checkbox].pristine, input[type=checkbox].pristine:before {
	animation: none;
}
/* Dark mode */
@media (prefers-color-scheme: dark) {
	body, input[type=checkbox]:before {
		background-image:
			linear-gradient(90deg,#3a3d46 2px,#3a3d4600 2px),
			linear-gradient(#3a3d46 2px,#2e3138 2px);
	}
	input[type=checkbox] {
		--off: #5c6270;
		--mid: #3d5fb6;
	}
}
/* Animations */
@keyframes bgOff {
	from {
		background-color: var(--on);
	}
	50% {
		background-color: var(--mid);
	}
	to {
		background-color: var(--off);
	}
}
@keyframes bgOn {
	from {
		background-color: var(--off);
	}
	50% {
		background-color: var(--mid);
	}
	to {
		background-color: var(--on);
	}
}
@keyframes handleOff {
	from {
		box-shadow:
			0 0.175em 0.175em 0 #0001 inset,
			0 0.375em 0 #0002 inset,
			0 0.375em 0 var(--on) inset,
			0 0.475em 0.1em #0001 inset;
		left: 1.125em;
		width: 1em;
	}
	50% {
		box-shadow:
			0 0.175em 0.175em 0 #0001 inset,
			0 0.375em 0 #0002 inset,
			0 0.375em 0 var(--mid) inset,
			0 0.475em 0.1em #0001 inset;
		left: 0.125em;
		width: 2em;
	}
	to {
		box-shadow:
			0 0.175em 0.175em 0 #0001 inset,
			0 0.375em 0 #0002 inset,
			0 0.375em 0 var(--off) inset,
			0 0.475em 0.1em #0001 inset;
		left: 0.125em;
		width: 1em;
	}
}
@keyframes handleOn {
	from {
		box-shadow:
			0 0.175em 0.175em 0 #0001 inset,
			0 0.375em 0 #0002 inset,
			0 0.375em 0 var(--off) inset,
			0 0.475em 0.1em #0001 inset;
		left: 0.125em;
		width: 1em;
	}
	50% {
		box-shadow:
			0 0.175em 0.175em 0 #0001 inset,
			0 0.375em 0 #0002 inset,
			0 0.375em 0 var(--mid) inset,
			0 0.475em 0.1em #0001 inset;
		left: 0.125em;
		width: 2em;
	}
	to {
		box-shadow:
			0 0.175em 0.175em 0 #0001 inset,
			0 0.375em 0 #0002 inset,
			0 0.375em 0 var(--on) inset,
			0 0.475em 0.1em #0001 inset;
		left: 1.125em;
		width: 1em;
	}
}
```
```javascript
// for removing the `pristine` class that prevents CSS animation on load
document.addEventListener("click",e => {
	let tar = e.target;
	if (tar.name == "toggle")
		tar.removeAttribute("class");
});
```
===


16. Pure CSS drawer

Pure CSS Drawers


The pure CSS drawer by Jhey creates a 3D drawer with panels that open when clicked. The third drawer has a wavy text animation inside it, further enhancing the awesomeness of this animation.

```pug
mixin drawer(position)
  .chest__drawer.drawer(data-position=position)
    details
      summary
    .drawer__structure
      .drawer__panel.drawer__panel--back
        block
      .drawer__panel.drawer__panel--bottom
      .drawer__panel.drawer__panel--right
      .drawer__panel.drawer__panel--left
      .drawer__panel.drawer__panel--front
.chest
  .chest__panel.chest__panel--back
  .chest__panel.chest__panel--top
  .chest__panel.chest__panel--bottom
  .chest__panel.chest__panel--right
  .chest__panel.chest__panel--front
    .chest__panel.chest__panel--front-frame
  .chest__panel.chest__panel--left
  +drawer(1)
    span CSS
  +drawer(2)
    span is
  +drawer(3)
    - for (const letter of "Awesome".split(''))
      span.letter= letter
```
```stylus
*
*:after
*:before
  box-sizing border-box
  transform-style preserve-3d
  will-change transform

:root
  --bg hsl(210, 32%, 80%)
  --hover 0.05
  --default 0.01
  --limit 0.9
  --height 30
  --width 20
  --depth 15
  --frame 1
  --handle hsl(0, 0%, 80%)
  --hue 10
  --saturation 0%
  --drawer-one hsl(0, 0%, 98%)
  --drawer-two hsl(0, 0%, 90%)
  --drawer-three hsl(0, 0%, 95%)
  --unit-one 'hsl(%s, %s, 50%)' % (var(--hue) var(--saturation))
  --unit-two 'hsl(%s, %s, 40%)' % (var(--hue) var(--saturation))
  --unit-three 'hsl(%s, %s, 20%)' % (var(--hue) var(--saturation))
  --unit-four 'hsl(%s, %s, 15%)' % (var(--hue) var(--saturation))
  --transition 0.2s

body
  background var(--bg)
  display grid
  place-items center
  min-height 100vh
  transform scale(1.5)

.chest
  height calc(var(--height) * 1vmin)
  transform translate3d(0, 0, 50vmin) rotateX(-32deg) rotateY(40deg)
  width calc(var(--width) * 1vmin)
  color hsl(0, 10%, 10%)

  &__panel
    position absolute

    &--back
      background var(--unit-two)

    &--back
    &--front
      height 100%
      width 100%
      transform translate3d(0, 0, calc(var(--depth) * var(--coefficient)))

    &--front-frame
      height 100%
      width 100%
      border calc(var(--frame) * 1vmin) solid var(--unit-one)
      border-bottom-width calc(var(--frame) * 2vmin)
      transform translate3d(0, 0, 0)

      &:after
      &:before
        content ''
        background var(--unit-one)
        height calc(var(--frame) * 1.5vmin)
        width calc(var(--width) * 1vmin)
        position absolute
        transform translate(-50%, -50%)
        left 50%

      &:after
        top calc(100 / 3 * 1.01%)

      &:before
        // Tiny little adjustment to cover the hole.
        top calc(100 / 3 * 2.01%)

    &--front
      --coefficient 0.5vmin
    &--back
      --coefficient -0.5vmin

    &--left
    &--right
      height 100%
      left 50%
      width calc(var(--depth) * 1vmin)
      background var(--unit-three)
      transform translate(-50%, 0) rotateY(90deg) translate3d(0, 0, calc(var(--width) * var(--coefficient)))

    &--right
      width calc((var(--depth) * 1vmin) + 2px)
      --coefficient 0.5vmin

    &--left
      --coefficient -0.5vmin

    &--top
    &--bottom
      height calc(var(--depth) * 1vmin)
      width calc(var(--width) * 1vmin)
      background var(--unit-two)

    &--top
      top 0
      width calc((var(--width) * 1vmin) + 0.1vmin)
      height calc((var(--depth) * 1vmin) + 0.1vmin)
      left 50%
      transform translate(-50%, -50%) rotateX(-90deg)

    &--bottom
      bottom 0
      transform translate(0, 50%) rotateX(-90deg)

  &__drawer
    --drawer-height calc((var(--height) - (5 * var(--frame))) / 3)
    position absolute
    top var(--top, 0)
    left 50%
    height calc(var(--drawer-height) * 1vmin)
    width calc((var(--width) - (2 * var(--frame))) * 1vmin)
    transform translate3d(-50%, 0, calc((var(--depth) * 0.5vmin) + 0.01vmin))

    &[data-position="1"]
      --index 1
      --top calc(var(--frame) * 1vmin)
    &[data-position="2"]
      --index 2
      --top calc(((2 * var(--frame)) + var(--drawer-height)) * 1vmin)
    &[data-position="3"]
      --index 3
      --top calc(((3 * var(--frame)) + (2 * var(--drawer-height))) * 1vmin)

.drawer
  &__structure
    height 100%
    width 100%
    position absolute
    top 0
    left 0

  &__panel
    position absolute

    &--left
    &--right
      width calc(var(--depth) * 1vmin)
      height 65%
      background var(--drawer-two)
      bottom 1%

    &--left
      // left calc((var(--frame) * 1vmin) + 1px)
      left 0
      transform-origin 0 50%
      transform rotateY(90deg)

    &--right
      right calc((var(--frame) * 1vmin) + 1px)
      right 0
      transform-origin 100% 50%
      transform rotateY(-90deg)

    &--front
      height calc((var(--drawer-height) + (0.6 * var(--frame))) * 1vmin)
      width calc((var(--width) - (0.6 * var(--frame))) * 1vmin)
      top 50%
      left 50%
      transform translate3d(-50%, -50%, 1px)
      background var(--unit-four)

    &--bottom
    &--back
      width calc(100% - (2vmin * var(--frame)))
      width 100%

    &--bottom
      height calc(var(--depth) * 1vmin)
      background var(--drawer-three)
      bottom 5%
      left 50%
      transform-origin 50% 100%
      transform translate(-50%, 0) rotateX(90deg)

    &--back
      height 65%
      background var(--drawer-one)
      bottom 1%
      left 50%
      transform translate3d(-50%, 0, calc(var(--depth) * -1vmin))
      text-align center
      line-height calc(var(--drawer-height) * 0.65vmin)
      font-size 3vmin
      font-family sans-serif
      font-weight bold

.letter
  color 'hsl(%s, 80%, 50%)' % var(--hue)
  display inline-block
  animation wave calc(var(--open) * 1s) calc(var(--delay) * -0.1s) infinite ease-in-out

  &:nth-of-type(1)
    --hue 15
    --delay 0
  &:nth-of-type(2)
    --hue 35
    --delay 1
  &:nth-of-type(3)
    --hue 45
    --delay 2
  &:nth-of-type(4)
    --hue 90
    --delay 3
  &:nth-of-type(5)
    --hue 180
    --delay 4
  &:nth-of-type(6)
    --hue 260
    --delay 5
  &:nth-of-type(7)
    --hue 320
    --delay 6

@keyframes wave
  0%, 100%
    transform translate3d(0, 10%, 1px)
  50%
    transform translate3d(0, -10%, 1px)

details
  position absolute
  height 100%
  width 100%
  top 0
  left 0
  cursor pointer
  outline transparent


// Responsible for opening/closing
details
  &:hover:not([open])
  &:hover:not([open]) + .drawer__structure
    --open var(--hover)

details
.drawer__structure
  transition transform var(--transition)

  &[open]
  &[open] + .drawer__structure
    --open var(--limit)

  transform translate3d(0, 0, calc((var(--open, var(--default)) * var(--depth)) * 1vmin))

summary
  outline transparent
  height 100%
  width 100%

  &::-webkit-details-marker
    display none

  &:after
    content ''
    position absolute
    background linear-gradient(var(--handle), var(--handle)) 50% 15% / 40% 8% no-repeat, transparent
    height 110%
    width 110%
    top 50%
    left 50%
    transform translate3d(-50%, -50%, 0.5vmin)


```

===


17. Copy button click effect

Copy Button Click Effect

The copy button click effect by Arjun Ace uses two SVG elements to create its animation. When clicked, a copy-like motion occurs. This effect relies on two keyframe definitions: press to give the card a pressed-in feel when clicked and bounce to animate the SVG copy icon out of view.

```html
 <div class="icon-conatiner">
    <?xml version="1.0" encoding="UTF-8"?>
    <svg width="19px" height="21px" viewBox="0 0 19 21" version="1.1" xmlns="http://www.w3.org/2000/svg"
      xmlns:xlink="http://www.w3.org/1999/xlink">
      <title>Group</title>
      <g id="Page-1" stroke="none" stroke-width="1" fill="none" fill-rule="evenodd">
        <g id="Artboard" transform="translate(-142.000000, -122.000000)">
          <g id="Group" transform="translate(142.000000, 122.000000)">
            <path
              d="M3.4,4 L11.5,4 L11.5,4 L16,8.25 L16,17.6 C16,19.4777681 14.4777681,21 12.6,21 L3.4,21 C1.52223185,21 6.74049485e-16,19.4777681 0,17.6 L0,7.4 C2.14128934e-16,5.52223185 1.52223185,4 3.4,4 Z"
              id="Rectangle-Copy" fill="#C4FFE4"></path>
            <path
              d="M6.4,0 L12,0 L12,0 L19,6.5 L19,14.6 C19,16.4777681 17.4777681,18 15.6,18 L6.4,18 C4.52223185,18 3,16.4777681 3,14.6 L3,3.4 C3,1.52223185 4.52223185,7.89029623e-16 6.4,0 Z"
              id="Rectangle" fill="#85EBBC"></path>
            <path d="M12,0 L12,5.5 C12,6.05228475 12.4477153,6.5 13,6.5 L19,6.5 L19,6.5 L12,0 Z" id="Path-2"
              fill="#64B18D"></path>
          </g>
        </g>
      </g>
    </svg>
    <svg width="19px" height="21px" viewBox="0 0 19 21" version="1.1" xmlns="http://www.w3.org/2000/svg"
      xmlns:xlink="http://www.w3.org/1999/xlink">
      <title>Group</title>
      <g id="Page-1" stroke="none" stroke-width="1" fill="none" fill-rule="evenodd">
        <g id="Artboard" transform="translate(-142.000000, -122.000000)">
          <g id="Group" transform="translate(142.000000, 122.000000)">
            <path
              d="M3.4,4 L11.5,4 L11.5,4 L16,8.25 L16,17.6 C16,19.4777681 14.4777681,21 12.6,21 L3.4,21 C1.52223185,21 6.74049485e-16,19.4777681 0,17.6 L0,7.4 C2.14128934e-16,5.52223185 1.52223185,4 3.4,4 Z"
              id="Rectangle-Copy" fill="#C4FFE4"></path>
            <path
              d="M6.4,0 L12,0 L12,0 L19,6.5 L19,14.6 C19,16.4777681 17.4777681,18 15.6,18 L6.4,18 C4.52223185,18 3,16.4777681 3,14.6 L3,3.4 C3,1.52223185 4.52223185,7.89029623e-16 6.4,0 Z"
              id="Rectangle" fill="#85EBBC"></path>
            <path d="M12,0 L12,5.5 C12,6.05228475 12.4477153,6.5 13,6.5 L19,6.5 L19,6.5 L12,0 Z" id="Path-2"
              fill="#64B18D"></path>
          </g>
        </g>
      </g>
    </svg>
  </div>
<div class="text">long press me</div>
```
```css
* {
  padding: 0;
  margin: 0;
  box-sizing: border-box;
}
body {
  width: 100%;
  height: 100vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: #c1ffe2;
}

.icon-conatiner {
  position: relative;
  width: 150px;
  height: 150px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #fff;
  border-radius: 15px;
  box-shadow: 20px 20px 15px 0 #ababab4d;
  cursor: pointer;
  /* animation: bounce 0.3s 1 forwards; */
}
svg {
  width: 50px;
  height: auto;
}
svg:last-child {
  position: absolute;
}

.icon-conatiner:active {
  animation: press 0.2s 1 linear;
}
.icon-conatiner:active svg:last-child {
  animation: bounce 0.2s 1 linear;
}
.text {
  color: #666;
  font-family: sans-serif;
  font-size: 16px;
  font-weight: bold;
  margin-top: 20px;
}
@keyframes press {
  0% {
    transform: scale(1);
  }
  50% {
    transform: scale(0.92);
  }
  to {
    transform: scale(1);
  }
}
@keyframes bounce {
  50% {
    transform: rotate(5deg) translate(20px, -50px);
  }
  to {
    transform: scale(0.9) rotate(10deg) translate(50px, -80px);
    opacity: 0;
  }
}

```
===


18. Beveled buttons

SCSS Beveled Buttons


The beveled buttons animation by Brandon McConnell creates a button that transforms from an envelope-like button to a ghost button when clicked. At the same time, the “Bevel Up” text moves from its initial position above the button to the back.

```html
<input type="checkbox" name="button" id="button">
<label class="bevel" for="button">Get Bevel’d</label>
<span>Bevel Up</span>
```
```scss
$red: #f33;
$red2: desaturate(darken($red, 15%), 18%);
$buttonHeight: 54;
html, body {
	display: flex;
	align-items: center;
	justify-content: center;
	flex-direction: column-reverse;
	width: 100%;
	height: 100%;
	font-family: "Chewy";
	-webkit-font-smoothing: antialiased;
	-moz-osx-font-smoothing: grayscale;
	transform: scale(1.25);
	overflow: hidden;
	@media (max-width: 320px) { transform: scale(1.125); }
	@media (max-width: 280px) { transform: scale(1.0625); }
	@media (max-width: 220px) { transform: scale(1); }
	@media (max-width: 200px) { transform: scale(0.9); }
	white-space: nowrap;
	user-select: none;
}
input { display: none; }
label { cursor: pointer; }
.bevel {
	display: flex;
	align-items: center;
	justify-content: center;
	position: relative;
	padding: 0 #{$buttonHeight/2 + 5 + "px"};
	// ^ 5px buffer between triangles and text
	height: #{$buttonHeight + "px"};
	background: $red linear-gradient(to bottom, $red 50%, $red2 50%);
	border-radius: 0px;
	box-shadow:
		      0px  15px 25px -10px rgba(#000, 0.5),
		inset 0px   0px  0px   0px rgba($red, 0),
		inset 0px -27px  0px   0px $red2;
	font-size: 20px;
	text-align: center;
	text-shadow:
		 0px 3px  5px $red2,
		 0px 5px 10px $red2,
		 0px 5px 10px $red2,
		 0px 5px 10px $red2,
		 0px 5px 10px $red2;
	color: #fff;
	letter-spacing: 0.4px;
	overflow: hidden;
	outline: 0 !important;
	transform: scale(1.01);
	transition: all 0.2s ease-in-out;
	&:before, &:after {
		content: " ";
		width: #{$buttonHeight/2 + "px"};
		max-width: 50%;
		height: 100%;
		position: absolute;
		top: 0;
		transition: all 0.135s ease-in-out;
		transition: all 0.135s cubic-bezier(0.74, 0.16, 0.24, 0.92);
		transition-delay: 0.15s;
	}
	&:before {
		left: 0;
		background:
			linear-gradient(45deg, transparent calc(200% / 3), rgba($red, 1) calc(200% / 3)),
			linear-gradient(-45deg, rgba($red2, 1) calc(100% / 3), transparent calc(100% / 3)),
			linear-gradient(to right, $red, $red2);
	}
	&:after {
		right: 0;
		background:
			linear-gradient(-45deg, transparent calc(200% / 3), rgba($red, 1) calc(200% / 3)),
			linear-gradient(45deg, rgba($red2, 1) calc(100% / 3), transparent calc(100% / 3)),
			linear-gradient(to left, $red, $red2);
	}
	&:hover {
		transform: scale(1.07);
		box-shadow:
			      0px  25px 40px -5px rgba(#000, 0.3),
			inset 0px   0px  0px  0px rgba($red, 0),
			inset 0px -27px  0px  0px $red2;
	}	
	input:checked ~ & {
		padding: 0 #{$buttonHeight/4 + 5 + "px"};
		background: rgba($red, 0);
		box-shadow:
				  0px 5px 15px -5px rgba(#000, 0.3),
			inset 0px 0px  0px  4px $red,
			inset 0px 0px  0px  0px $red2;
		border-radius: 10px;
		font-size: 24px;
		font-weight: 700;
		text-shadow:
			 0px 3px  5px rgba($red2, 0),
			 0px 5px 10px rgba($red2, 0),
			 0px 5px 10px rgba($red2, 0),
			 0px 5px 10px rgba($red2, 0),
			 0px 5px 10px rgba($red2, 0);
		color: $red;
		transform: scale(0.7) rotate(-6deg);
		transition: all 0.25s ease-in-out, text-shadow 0s ease-in-out, box-shadow 0.25s ease-in-out;
		transition: all 0.45s cubic-bezier(0.6, -0.9, 0.39, 1.65), text-shadow 0s ease-in-out, box-shadow 0.45s cubic-bezier(0.6, -0.9, 0.39, 1.65);
		transition-delay: 0.135s;
		&:before, &:after { transition-delay: 0s; }
		&:before { transform: translateX(-100%); }
		&:after { transform: translateX(100%); }
		+ span {
			// opacity: 0.15;
			color: rgba($red, 0.1875);
			transform: translateY(56px);
		}
	}
}
span {
	display: block;
	margin-bottom: 10px;
	font-family: "Didact Gothic";
	font-size: 32px;
	font-weight: 700;
	color: #222;
	z-index: -1;
	transition: all 0.25s ease-in-out;
	transition: all 0.45s cubic-bezier(0.74, 0.16, 0.24, 0.92);
	transition-delay: 0.135s;
}
```
===


19. Biometrics button

Black Biometirics Button

The biometrics button by Mikael Ainalem creates an interactive login button with a fingerprint animation. When the button is clicked, it transforms from displaying the text "LOGIN," to showing an SVG fingerprint animation that traces different curved path lines to mimic a fingerprint scan.

Among other CSS features, the animation relies on stroke-dasharray and stroke-dashoffset to manipulate the SVG’s path and give the illusion of drawing fingerprint lines. It also uses a text keyframe to manage the text’s visibility and the Ok keyframe to control the SVG animation.

```html
<div class="container" onclick="this.classList.toggle('active')">
  <span class="text">LOGIN</span>
  <svg class="fingerprint fingerprint-base" xmlns="http://www.w3.org/2000/svg" width="100" height="100" viewBox="0 0 100 100">
    <g class="fingerprint-out" fill="none" stroke-width="2" stroke-linecap="round">
      <path
            class="odd" d="m 25.117139,57.142857 c 0,0 -1.968558,-7.660465 -0.643619,-13.149003 1.324939,-5.488538 4.659682,-8.994751 4.659682,-8.994751" />
      <path
            class="odd" d="m 31.925369,31.477584 c 0,0 2.153609,-2.934998 9.074971,-5.105078 6.921362,-2.17008 11.799844,-0.618718 11.799844,-0.618718" />
      <path
            class="odd" d="m 57.131213,26.814448 c 0,0 5.127709,1.731228 9.899495,7.513009 4.771786,5.781781 4.772971,12.109204 4.772971,12.109204" />
      <path
            class="odd" d="m 72.334009,50.76769 0.09597,2.298098 -0.09597,2.386485" />
      <path
            class="even" d="m 27.849282,62.75 c 0,0 1.286086,-1.279223 1.25,-4.25 -0.03609,-2.970777 -1.606117,-7.675266 -0.625,-12.75 0.981117,-5.074734 4.5,-9.5 4.5,-9.5" />
      <path
            class="even" d="m 36.224282,33.625 c 0,0 8.821171,-7.174484 19.3125,-2.8125 10.491329,4.361984 11.870558,14.952665 11.870558,14.952665" />
      <path
            class="even" d="m 68.349282,49.75 c 0,0 0.500124,3.82939 0.5625,5.8125 0.06238,1.98311 -0.1875,5.9375 -0.1875,5.9375" />
      <path
            class="odd" d="m 31.099282,65.625 c 0,0 1.764703,-4.224042 2,-7.375 0.235297,-3.150958 -1.943873,-9.276886 0.426777,-15.441942 2.370649,-6.165056 8.073223,-7.933058 8.073223,-7.933058" />
      <path
            class="odd" d="m 45.849282,33.625 c 0,0 12.805566,-1.968622 17,9.9375 4.194434,11.906122 1.125,24.0625 1.125,24.0625" />
      <path
            class="even" d="m 59.099282,70.25 c 0,0 0.870577,-2.956221 1.1875,-4.5625 0.316923,-1.606279 0.5625,-5.0625 0.5625,-5.0625" />
      <path
            class="even" d="m 60.901059,56.286612 c 0,0 0.903689,-9.415996 -3.801777,-14.849112 -3.03125,-3.5 -7.329245,-4.723939 -11.867187,-3.8125 -5.523438,1.109375 -7.570313,5.75 -7.570313,5.75" />
      <path
            class="even" d="m 34.072577,68.846248 c 0,0 2.274231,-4.165782 2.839205,-9.033748 0.443558,-3.821814 -0.49394,-5.649939 -0.714206,-8.05386 -0.220265,-2.403922 0.21421,-4.63364 0.21421,-4.63364" />
      <path
            class="odd" d="m 37.774165,70.831845 c 0,0 2.692139,-6.147592 3.223034,-11.251208 0.530895,-5.103616 -2.18372,-7.95562 -0.153491,-13.647655 2.030229,-5.692035 8.108442,-4.538898 8.108442,-4.538898" />
      <path
            class="odd" d="m 54.391174,71.715729 c 0,0 2.359472,-5.427681 2.519068,-16.175068 0.159595,-10.747388 -4.375223,-12.993087 -4.375223,-12.993087" />
      <path
            class="even" d="m 49.474282,73.625 c 0,0 3.730297,-8.451831 3.577665,-16.493718 -0.152632,-8.041887 -0.364805,-11.869326 -4.765165,-11.756282 -4.400364,0.113044 -3.875,4.875 -3.875,4.875" />
      <path
            class="even" d="m 41.132922,72.334447 c 0,0 2.49775,-5.267079 3.181981,-8.883029 0.68423,-3.61595 0.353553,-9.413359 0.353553,-9.413359" />
      <path
            class="odd" d="m 45.161782,73.75 c 0,0 1.534894,-3.679847 2.40625,-6.53125 0.871356,-2.851403 1.28125,-7.15625 1.28125,-7.15625" />
      <path
            class="odd" d="m 48.801947,56.125 c 0,0 0.234502,-1.809418 0.109835,-3.375 -0.124667,-1.565582 -0.5625,-3.1875 -0.5625,-3.1875" />
    </g>
  </svg>
  <svg class="fingerprint fingerprint-active" xmlns="http://www.w3.org/2000/svg" width="100" height="100" viewBox="0 0 100 100">
    <g class="fingerprint-out" fill="none" stroke-width="2" stroke-linecap="round">
      <path
            class="odd" d="m 25.117139,57.142857 c 0,0 -1.968558,-7.660465 -0.643619,-13.149003 1.324939,-5.488538 4.659682,-8.994751 4.659682,-8.994751" />
      <path
            class="odd" d="m 31.925369,31.477584 c 0,0 2.153609,-2.934998 9.074971,-5.105078 6.921362,-2.17008 11.799844,-0.618718 11.799844,-0.618718" />
      <path
            class="odd" d="m 57.131213,26.814448 c 0,0 5.127709,1.731228 9.899495,7.513009 4.771786,5.781781 4.772971,12.109204 4.772971,12.109204" />
      <path
            class="odd" d="m 72.334009,50.76769 0.09597,2.298098 -0.09597,2.386485" />
      <path
            class="even" d="m 27.849282,62.75 c 0,0 1.286086,-1.279223 1.25,-4.25 -0.03609,-2.970777 -1.606117,-7.675266 -0.625,-12.75 0.981117,-5.074734 4.5,-9.5 4.5,-9.5" />
      <path
            class="even" d="m 36.224282,33.625 c 0,0 8.821171,-7.174484 19.3125,-2.8125 10.491329,4.361984 11.870558,14.952665 11.870558,14.952665" />
      <path
            class="even" d="m 68.349282,49.75 c 0,0 0.500124,3.82939 0.5625,5.8125 0.06238,1.98311 -0.1875,5.9375 -0.1875,5.9375" />
      <path
            class="odd" d="m 31.099282,65.625 c 0,0 1.764703,-4.224042 2,-7.375 0.235297,-3.150958 -1.943873,-9.276886 0.426777,-15.441942 2.370649,-6.165056 8.073223,-7.933058 8.073223,-7.933058" />
      <path
            class="odd" d="m 45.849282,33.625 c 0,0 12.805566,-1.968622 17,9.9375 4.194434,11.906122 1.125,24.0625 1.125,24.0625" />
      <path
            class="even" d="m 59.099282,70.25 c 0,0 0.870577,-2.956221 1.1875,-4.5625 0.316923,-1.606279 0.5625,-5.0625 0.5625,-5.0625" />
      <path
            class="even" d="m 60.901059,56.286612 c 0,0 0.903689,-9.415996 -3.801777,-14.849112 -3.03125,-3.5 -7.329245,-4.723939 -11.867187,-3.8125 -5.523438,1.109375 -7.570313,5.75 -7.570313,5.75" />
      <path
            class="even" d="m 34.072577,68.846248 c 0,0 2.274231,-4.165782 2.839205,-9.033748 0.443558,-3.821814 -0.49394,-5.649939 -0.714206,-8.05386 -0.220265,-2.403922 0.21421,-4.63364 0.21421,-4.63364" />
      <path
            class="odd" d="m 37.774165,70.831845 c 0,0 2.692139,-6.147592 3.223034,-11.251208 0.530895,-5.103616 -2.18372,-7.95562 -0.153491,-13.647655 2.030229,-5.692035 8.108442,-4.538898 8.108442,-4.538898" />
      <path
            class="odd" d="m 54.391174,71.715729 c 0,0 2.359472,-5.427681 2.519068,-16.175068 0.159595,-10.747388 -4.375223,-12.993087 -4.375223,-12.993087" />
      <path
            class="even" d="m 49.474282,73.625 c 0,0 3.730297,-8.451831 3.577665,-16.493718 -0.152632,-8.041887 -0.364805,-11.869326 -4.765165,-11.756282 -4.400364,0.113044 -3.875,4.875 -3.875,4.875" />
      <path
            class="even" d="m 41.132922,72.334447 c 0,0 2.49775,-5.267079 3.181981,-8.883029 0.68423,-3.61595 0.353553,-9.413359 0.353553,-9.413359" />
      <path
            class="odd" d="m 45.161782,73.75 c 0,0 1.534894,-3.679847 2.40625,-6.53125 0.871356,-2.851403 1.28125,-7.15625 1.28125,-7.15625" />
      <path
            class="odd" d="m 48.801947,56.125 c 0,0 0.234502,-1.809418 0.109835,-3.375 -0.124667,-1.565582 -0.5625,-3.1875 -0.5625,-3.1875" />
    </g>
  </svg>
  <svg class="ok" xmlns="http://www.w3.org/2000/svg" width="100" height="100" viewBox="0 0 100 100"><path d="M34.912 50.75l10.89 10.125L67 36.75" fill="none" stroke="#fff" stroke-width="6"/></svg>
</div>
```
```css
body {
  align-items: center;
  display: flex;
  font-family: sans-serif;
  font-weight: 600;
  font-size: 26px;
  height: 100vh;
  justify-content: center;
  margin: 0;
}
.container {
  align-items: center;
  background: #000;
  border-radius: 40px;
  box-shadow: 0 14px 28px rgba(0,0,0,0.25), 0 10px 10px rgba(0,0,0,0.22);
  display: flex;
  height: 80px;
  justify-content: center;
  position: relative;
  width: 200px;
}
.text {
  color: white;
  position: absolute;
  transition: opacity 300ms;
  user-select: none;
  -moz-user-select: none;
}
.fingerprint {
  /* height: 80px; */
  left: -8px;
  opacity: 0;
  position: absolute;
  stroke: #777;
  top: -9px;
  transition: opacity 1ms;
}
.fingerprint-active {
  stroke: #fff;
}
.fingerprint-out {
  opacity: 1;
}
.odd {
  stroke-dasharray: 0px 50px;
  stroke-dashoffset: 1px;
  transition: stroke-dasharray 1ms;
}
.even {
  stroke-dasharray: 50px 50px;
  stroke-dashoffset: -41px;
  transition: stroke-dashoffset 1ms;
}
.ok {
  opacity: 0;
}
.active.container {
  animation: 6s Container;
}
.active .text {
  opacity: 0;
  animation: 6s Text forwards;
}
.active .fingerprint {
  opacity: 1;
  transition: opacity 300ms 200ms;
}
.active .fingerprint-base .odd {
  stroke-dasharray: 50px 50px;
  transition: stroke-dasharray 800ms 100ms;
}
.active .fingerprint-base .even {
  stroke-dashoffset: 0px;
  transition: stroke-dashoffset 800ms;
}
.active .fingerprint-active .odd {
  stroke-dasharray: 50px 50px;
  transition: stroke-dasharray 2000ms 1500ms;
}
.active .fingerprint-active .even {
  stroke-dashoffset: 0px;
  transition: stroke-dashoffset 2000ms 1300ms;
}
.active .fingerprint-out {
  opacity: 0;
  transition: opacity 300ms 4100ms;
}
.active .ok {
  opacity: 1;
  animation: 6s Ok forwards;
}
@keyframes Container {
  0% { width: 200px }
  6% { width: 80px }
  71% { transform: scale(1); }
  75% { transform: scale(1.2); }
  77% { transform: scale(1); }

  94% { width: 80px }
  100% { width: 200px }
}
@keyframes Text {
  0% { opacity: 1; transform: scale(1); }
  6% { opacity: 0; transform: scale(0.5); }

  94% { opacity: 0; transform: scale(0.5); }
  100% { opacity: 1; transform: scale(1); }
}
@keyframes Ok {
  0% { opacity: 0 }
  70% { opacity: 0; transform: scale(0); }
  75% { opacity: 1; transform: scale(1.1); }
  77% { opacity: 1; transform: scale(1); }
  92% { opacity: 1; transform: scale(1); }
  96% { opacity: 0; transform: scale(0.5); }
  100% { opacity: 0 }
}
```
```javascript
const container = document.querySelector('.container')
container.addEventListener('animationend', () => {
  container.classList.remove('active');
});
```
===

Loading animations


===


20. Kinetic CSS loaders

Kinetic CSS loaders

The kinetic CSS loaders by Jenning use a combination of keyframes, pseudo-elements, and CSS properties to create loading elements. You can use these loaders to add visual feedback when processes are running in the background of your app.

```html
<div class="page">
	<header class="header">
		<h1 class="header-title">Kinetic CSS loaders</h1>
		<p class="header-subtitle">single html element css animation</p>
	</header>

	<main class="container">
		<div class="item">
			<i class="loader loader--2"></i>
		</div>
		<div class="item">
			<i class="loader loader--3"></i>
		</div>
		<div class="item">
			<i class="loader loader--1"></i>
		</div>

		<div class="item">
			<i class="loader loader--7"></i>
		</div>
		<div class="item">
			<i class="loader loader--9"></i>
		</div>
		<div class="item">
			<i class="loader loader--8"></i>
		</div>

		<div class="item">
			<i class="loader loader--5"></i>
		</div>
		<div class="item">
			<i class="loader loader--4"></i>
		</div>
		<div class="item">
			<i class="loader loader--6"></i>
		</div>
	</main>
</div>
```
```css
.loader {
	--loader-size: calc(var(--block-size) / 2);
	--loader-size-half: calc(var(--loader-size) / 2);
	--loader-size-half-neg: calc(var(--loader-size-half) * -1);
	--light-color: rgba(255, 255, 255, 0.3);
  --dot-size: 5px;
  --dot-size-half: calc(var(--dot-size) / 2);
  --dot-size-half-neg: calc(var(--dot-size-half) * -1);
	
	display: block;
	position: relative;
	width: var(--loader-size);
	display: grid;
	place-items: center;
	color: white;
}

.loader::before,
.loader::after {
	content: '';
	position: absolute;
}

/**
	loader--1
**/
.loader--1 {
	--loader-size: calc(var(--block-size) / 3);
	--anim-duration: 1s;
	--loader-1-dist: calc(var(--loader-size) - var(--dot-size-half) + 1px);
	aspect-ratio: 1 / 1;
	border: 1px solid var(--light-color);
}

.loader--1::before,
.loader--1::after {
	width: var(--dot-size);
	aspect-ratio: 1 / 1;
	background: currentColor;
	border-radius: 50%;
	top: var(--dot-size-half-neg);
	left: var(--dot-size-half-neg);
	animation: loader-1 var(--anim-duration) cubic-bezier(0.27, 0.08, 0.26, 0.7) infinite;
}

.loader--1::after {
	animation-delay: calc(var(--anim-duration) / 4 * -1);
}

@keyframes loader-1 {
	0%, 100% {
		transform: none;
	}
	
	25% {
		transform: translateX(var(--loader-1-dist));
	}
	
	50% {
		transform: translateX(var(--loader-1-dist)) translateY(var(--loader-1-dist));
	}
	
	75% {
		transform: translateX(0) translateY(var(--loader-1-dist));
	}
}

/**
	loader--2
**/
.loader--2 {
	--loader-size: calc(var(--block-size) / 3);
	--anim-duration: 1s;
	height: 1px;
	background-color: var(--light-color);
}

.loader--2::before,
.loader--2::after {
	width: var(--dot-size);
	aspect-ratio: 1 / 1;
	background: currentColor;
	border-radius: 50%;
	top: calc(var(--dot-size-half-neg) + 1px);
	left: var(--dot-size-half-neg);
	animation: loader-2 var(--anim-duration) cubic-bezier(0.27, 0.08, 0.26, 0.7) infinite;
}

.loader--2::after {
	animation-delay: calc(var(--anim-duration) / 3 * -1)
}

@keyframes loader-2 {
	0%, 100% {
		transform: none;
	}
	
	44% {
		transform: translateX(calc(var(--loader-size) + var(--dot-size-half)));
	}
}

/**
	loader--3
**/
.loader--3 {
	--loader-size: calc(var(--block-size) / 3);
	--anim-duration: 1.2s;
	aspect-ratio: 1 / 1;
	border: 1px solid var(--light-color);
	border-radius: 50%;
	animation: loader-3 calc(var(--anim-duration) * 3) linear infinite;
}

.loader--3::before,
.loader--3::after {
	width: var(--dot-size);
	aspect-ratio: 1 / 1;
	background: currentColor;
	border-radius: 50%;
	top: var(--dot-size-half-neg);
	left: calc(50% - var(--dot-size-half));
	animation: loader-3 var(--anim-duration) cubic-bezier(0.27, 0.08, 0.26, 0.7) infinite;
	transform-origin: center calc(var(--loader-size-half) + var(--dot-size-half) - 1px);
}

.loader--3::after {
	animation-delay: calc(var(--anim-duration) / 3 * -1);
}

@keyframes loader-3 {
	100% {
		transform: rotate(1turn);
	}
}

/**
	loader--4
**/
.loader--4 {
	--anim-duration: 0.5s;
	aspect-ratio: 1 / 1;
	perspective: 50vmin;
	transform-style: preserve-3d;
	transform: rotateX(55deg);
}

.loader--4::before,
.loader--4::after {
	width: 50%;
	aspect-ratio: 1 / 1;
	border: 1px solid currentColor;
	top: 25%;
	left: 25%;
	border-radius: 50%;
	animation: loader-4 var(--anim-duration) cubic-bezier(0.07, 0.59, 0.56, 0.88) infinite;
}

.loader--4::after {
	animation-delay: calc(var(--anim-duration) / 2 * -1);
}

@keyframes loader-4 {
	0% {
		transform: scale(0.2) translateZ(-8vmin);
	}
	
	0%, 100% {
		opacity: 0;
	}
	
	66% {
		opacity: 0.8;
		transform: scale(1.2) translateZ(6vmin);
	}
	
	100% {
		transform: scale(1.8) translateZ(2vmin);
	}
}

/**
	loader--5
**/
.loader--5 {
	--tilt-deg: 40deg;
	--anim-duration: 0.6s;
	aspect-ratio: 1 / 1;
	perspective: 50vmin;
	transform-style: preserve-3d;
	animation: loader-5-1 calc(var(--anim-duration) * 3) linear alternate-reverse infinite;
}

.loader--5::before,
.loader--5::after {
	width: 50%;
	aspect-ratio: 1 / 1;
	background-color: currentColor;
	top: 25%;
	left: 25%;
	clip-path: polygon(50% 0, 100% 100%, 0 100%);
	animation: loader-5 var(--anim-duration) cubic-bezier(0.07, 0.59, 0.56, 0.88) infinite;
}

.loader--5::before {
	--turn-deg: 360deg;
}

.loader--5::after {
	--turn-deg: 410deg;
	animation-delay: calc(var(--anim-duration) / 1.8 * -1);
}

@keyframes loader-5 {
	0% {
		transform: scale(0.3) translateZ(-5vmin);
	}
	
	0%, 100% {
		opacity: 0;
	}
	
	66% {
		opacity: 0.8;
		transform: scale(1.2) translateZ(5vmin) rotate(var(--turn-deg));
	}
	
	100% {
		transform: scale(1) translateZ(3vmin) rotate(calc(var(--turn-deg) * 1.2));
	}
}

@keyframes loader-5-1 {
	0% {
		transform: rotateX(var(--tilt-deg)) rotateY(-15deg);
	}

	100% {
		transform: rotateX(var(--tilt-deg)) rotateY(15deg);
	}
}

/**
	loader--6
**/
.loader--6 {
	--loader-size: calc(var(--block-size) / 3);
	--anim-duration: 0.6s;
	aspect-ratio: 1 / 1;
	perspective: 50vmin;
	transform-style: preserve-3d;
	transform: rotateX(35deg);
}

.loader--6::before,
.loader--6::after {
	width: 50%;
	aspect-ratio: 1 / 1;
	background-color: currentColor;
	top: 25%;
	left: 25%;
	animation: loader-6 var(--anim-duration) cubic-bezier(0.07, 0.59, 0.56, 0.88) infinite;
}

.loader--6::before {
	--turn-deg: -60deg;
	--x-dist: -25%;
	transform-origin: left calc(var(--loader-size) * -1);
}

.loader--6::after {
	--turn-deg: 60deg;
	--x-dist: 25%;
	transform-origin: right calc(var(--loader-size) * -1);
	animation-delay: calc(var(--anim-duration) / 2 * -1);
}

@keyframes loader-6 {
	0% {
		transform: scale(0.3) translateZ(-15vmin) rotateY(calc(var(--turn-deg) * -1));
	}
	
	0%, 100% {
		opacity: 0;
	}
	
	33% {
		opacity: 0.8;
		transform: scale(1.2) translateZ(5vmin) translateX(var(--x-dist));
	}
	
	100% {
		transform: scale(1.2) translateZ(5vmin) translateX(var(--x-dist)) rotateY(var(--turn-deg));
	}
}

/**
	loader--7
**/
.loader--7 {
	--loader-size: calc(var(--block-size) / 3);
	--anim-duration: 0.8s;
	aspect-ratio: 1 / 1;
	border: 1px solid var(--light-color);
	border-radius: 50%;
	animation: loader-7 calc(var(--anim-duration) * 2) linear infinite;
}

.loader--7::before,
.loader--7::after {
	width: var(--dot-size);
	aspect-ratio: 1 / 1;
	background: currentColor;
	border-radius: 50%;
	top: calc(50% - var(--dot-size-half));
	left: calc(50% - var(--dot-size-half));
	animation: loader-7-1 var(--anim-duration) cubic-bezier(0.32, 0.41, 0.3, 1.87) infinite;
}

.loader--7::after {
	animation-name: loader-7-2;
	animation-delay: calc(var(--anim-duration) / 3 * -1);
}

@keyframes loader-7 {
	100% {
		transform: rotate(1turn);
	}
}

@keyframes loader-7-1 {
	0%, 100% {
		transform: translateX(var(--loader-size-half-neg));
	}
	
	55% {
		transform: translateX(var(--loader-size-half));
	}
}

@keyframes loader-7-2 {
	0%, 100% {
		transform: translateY(var(--loader-size-half-neg));
	}
	
	55% {
		transform: translateY(var(--loader-size-half));
	}
}

/**
	loader--8
**/
.loader--8 {
	--loader-size: calc(var(--block-size) / 3);
	--anim-duration: 0.8s;
	aspect-ratio: 1 / 1;
	border: 1px dashed var(--light-color);
	border-radius: 50%;
	perspective: 50vmin;
	transform-style: preserve-3d;
	transform: rotateX(45deg) rotateY(15deg);
}

.loader--8::before,
.loader--8::after {		
	animation: loader-8 var(--anim-duration) cubic-bezier(0.39, 0.24, 0, 0.99) infinite;
}

.loader--8::before {
	--z-dist: 8vmin;
	width: var(--dot-size);
	aspect-ratio: 1 / 1;
	background: currentColor;
	border-radius: 50%;
	top: calc(50% - var(--dot-size-half));
	left: calc(50% - var(--dot-size-half));
	animation-delay: calc(var(--anim-duration) / 4 * -1);
}

.loader--8::after {
	--z-dist: 4vmin;
	width: 65%;
	aspect-ratio: 1 / 1;
	border-radius: 50%;
	border: 1px solid currentColor;
}

@keyframes loader-8 {
	0%, 100% {
		transform: translateZ(calc(var(--z-dist) * -1)) scale(0.6);
	}
	
	55% {
		transform: translateZ(var(--z-dist));
	}
}


/**
	loader--9
**/
.loader--9 {
	--loader-size: calc(var(--block-size) / 6);
	--anim-duration: 0.6s;
	aspect-ratio: 1 / 1;
	border-radius: 50%;
	background-color: currentColor;
	box-shadow: 0 0 var(--loader-size) var(--light-color);
	animation: loader-9 calc(var(--anim-duration) * 6) linear infinite;
}

.loader--9::before,
.loader--9::after {
	width: var(--dot-size);
	aspect-ratio: 1 / 1;
	background: currentColor;
	border-radius: 50%;
	animation: loader-9-1 var(--anim-duration) cubic-bezier(0.27, 0.08, 0.26, 0.7) infinite;
}

.loader--9::before {
	--x-dist: 0;
	--y-dist: var(--loader-size-half);
	bottom: calc(100% + var(--loader-size));
	left: calc(50% - var(--dot-size-half));
	transform-origin: center var(--loader-size);
}

.loader--9::after {
	--x-dist: var(--loader-size-half);
	--y-dist: 0;
	top: calc(50% - var(--dot-size-half));
	right: calc(100% + var(--loader-size));
	transform-origin: var(--loader-size) center;
	animation-delay: calc(var(--anim-duration) / 2 * -1);
}

@keyframes loader-9 {
	100% {
		transform: rotate(1turn);
	}
}

@keyframes loader-9-1 {
	0%, 100% {
		opacity: 0;
	}
	
	33% {
		opacity: 1;
	}
	
	0% {
		transform: scale(1.1);
	}
	
	88% {
		transform: rotate(180deg) translate(var(--x-dist), var(--y-dist));
	}
}

/**
	miscs
**/

.container {
	--block-size: 18vmin;
	
	display: grid;
	grid-template-columns: repeat(3, var(--block-size));
	grid-template-rows: repeat(3, var(--block-size));
	grid-gap: 1vmin;
}

.item	{
	background: rgba(255, 255, 255, 0.1);
	display: grid;
	place-items: center;
	border-radius: 4px;
	transition: opacity 0.4s ease;
}

.container:hover .item {
	opacity: 0.3;
}

.container:hover .item:hover {
	opacity: 1;
}

.page {
	margin: auto;
}

.header {
	margin-bottom: 4vmin;
}

.header-title {
	font-size: 3.75vmin;
}

.header-subtitle {
	font-size: 2vmin;
	text-transform: uppercase;
	opacity: 0.6;
}

html, body {
	margin: 0;
	display: flex;
	width: 100%;
	height: 100%;
	font-family: 'Noto Sans', sans-serif;
	color: white;
	text-align: center;
	letter-spacing: 0.3px;
	background: linear-gradient(to right top, #F27121, #E94057, #8A2387);

}

*, *::before, *::after {
	box-sizing: border-box;
}
```
===


21. Newton’s cradle

Newton's cradle using only CSS

Loading interactive CodePen...

Open on CodePen
Newton’s cradle animation by Arushi Bajpai creates a pendulum swing that continually sways from one end to the other. Each .ball represents a pendulum bob, with a ::before pseudo-element styled to resemble the bob’s strange. The .first and .last balls are animated with firstball and lastball keyframes to mimic the natural swinging motion of a pendulum. The transform-origin (docs) ensures the balls rotate from the top point of their pseudo-element strings, creating a realistic pivot.
```html
<div class="pendulum">
  <div class="pendulum_box">
    <div class="ball first"></div>
      <div class="ball"></div>
      <div class="ball"></div>
      <div class="ball"></div>
      <div class="ball last"></div>
  </div>
</div>
```
```css
body{
  background-color: #98cfb2;
}
.pendulum{
  position:absolute; 
  width: 220px;
  height: 180px;
  background-color: #f8c6cf;
  top:50%;
  left:  50%;
  border-radius: 5%;
  align-items: center;
  border-top: 15px solid #eee7d5;
  transform: translate(-50%, -50%);
  }
.pendulum_box{
  display: flex;
  padding: 120px 0 0 10px;
  position: absolute;
  flex: 1;
}
.ball{
  height: 40px;
  width: 40px;
  border-radius: 50%;
  background-color: #455681;
  position: relative;
  transform-origin: 50% -300%;
  
}
  .ball::before{
    content: '';
    width: 2px;
    height: 120px;
    background-color: #fffeff;
    left: 18px;
    top: -120px;
    position: absolute;
  }

.ball.first{
  animation: firstball .9s alternate ease-in infinite;
}

@keyframes firstball{
  0%{
    transform: rotate(35deg);
  }
  50%{
    transform: rotate(0deg);
  }
}

.ball.last{
  animation: lastball .9s alternate ease-out infinite;
}

@keyframes lastball{
  50%{
    transform: rotate(0deg);
  }
  100%{
    transform: rotate(-35deg);
  }
}
```
===


22. Glowing loader ring

Glowing Loader Ring Animation

Loading interactive CodePen...

Open on CodePen
The glowing loader ring animation by Ekta maurya creates a creates a glowing, spinning ring effect. The .ring element forms the circular base while its :before pseudo-element imitates the yellow border. The animateC keyframe handles the yellow rim’s motion, while the animate keyframe handles the yellow dot’s motion.
```html
<div class="ring">Loading
  <span></span>
</div>
```
```css
body
{
  margin:0;
  padding:0;
  background:#262626;
}
.ring
{
  position:absolute;
  top:50%;
  left:50%;
  transform:translate(-50%,-50%);
  width:150px;
  height:150px;
  background:transparent;
  border:3px solid #3c3c3c;
  border-radius:50%;
  text-align:center;
  line-height:150px;
  font-family:sans-serif;
  font-size:20px;
  color:#fff000;
  letter-spacing:4px;
  text-transform:uppercase;
  text-shadow:0 0 10px #fff000;
  box-shadow:0 0 20px rgba(0,0,0,.5);
}
.ring:before
{
  content:'';
  position:absolute;
  top:-3px;
  left:-3px;
  width:100%;
  height:100%;
  border:3px solid transparent;
  border-top:3px solid #fff000;
  border-right:3px solid #fff000;
  border-radius:50%;
  animation:animateC 2s linear infinite;
}
span
{
  display:block;
  position:absolute;
  top:calc(50% - 2px);
  left:50%;
  width:50%;
  height:4px;
  background:transparent;
  transform-origin:left;
  animation:animate 2s linear infinite;
}
span:before
{
  content:'';
  position:absolute;
  width:16px;
  height:16px;
  border-radius:50%;
  background:#fff000;
  top:-6px;
  right:-8px;
  box-shadow:0 0 20px #fff000;
}
@keyframes animateC
{
  0%
  {
    transform:rotate(0deg);
  }
  100%
  {
    transform:rotate(360deg);
  }
}
@keyframes animate
{
  0%
  {
    transform:rotate(45deg);
  }
  100%
  {
    transform:rotate(405deg);
  }
}
```
===


23. Making-pancake loader

Never-ending box

Loading interactive CodePen...

Open on CodePen
The making-pancake loader by Pawel creates a playful animation of a flipping pancake in a pan, complete with bubbling effects to simulate cooking. The pancake-flipping effect is achieved through multiple keyframe animations:

jump: Handles the movement of the pancake
flip: Animates the pan’s flipping motion
switchSide: Rotates the pan around its Y-axis
fly: Simulates the pancake rising to its peak, rotating mid-air, and returning to its starting position
bubble: Controls the size, position, and fading of the bubbles as they rise from the pan
pulse: Adds a pulsating effect to the "Cooking in progress..." text by gradually scaling and fading the text in and out
```html
<div id="loader">
  <div id="box"></div>
  <div id="hill"></div>
</div>
```
```css
html,
body {
  background-color: #404456;
}

#loader {
  position: absolute;
  top: 50%;
  left: 50%;
  margin-top: -2.7em;
  margin-left: -2.7em;
  width: 5.4em;
  height: 5.4em;
  background-color: #404456;
}

#hill {
  position: absolute;
  width: 7.1em;
  height: 7.1em;
  top: 1.7em;
  left: 1.7em;
  background-color: transparent;
  border-left: .25em solid whitesmoke;
  transform: rotate(45deg);
}

#hill:after {
  content: '';
  position: absolute;
  width: 7.1em;
  height: 7.1em;
  left: 0;
  background-color: #404456;
}

#box {
  position: absolute;
  left: 0;
  bottom: -.1em;
  width: 1em;
  height: 1em;
  background-color: transparent;
  border: .25em solid whitesmoke;
  border-radius: 15%;
  transform: translate(0, -1em) rotate(-45deg);
  animation: push 2.5s cubic-bezier(.79, 0, .47, .97) infinite;
}

@keyframes push {
  0% {
    transform: translate(0, -1em) rotate(-45deg);
  }
  5% {
    transform: translate(0, -1em) rotate(-50deg);
  }
  20% {
    transform: translate(1em, -2em) rotate(47deg);
  }
  25% {
    transform: translate(1em, -2em) rotate(45deg);
  }
  30% {
    transform: translate(1em, -2em) rotate(40deg);
  }
  45% {
    transform: translate(2em, -3em) rotate(137deg);
  }
  50% {
    transform: translate(2em, -3em) rotate(135deg);
  }
  55% {
    transform: translate(2em, -3em) rotate(130deg);
  }
  70% {
    transform: translate(3em, -4em) rotate(217deg);
  }
  75% {
    transform: translate(3em, -4em) rotate(220deg);
  }
  100% {
    transform: translate(0, -1em) rotate(-225deg);
  }
}
```


===

24. Speedy truck

Speedy truck

The speedy truck animation by Tippy fodder creates a looping animation of a scenic background with a truck driving through a landscape of mountains, hills, trees, and rocks. This animation relies on the following keyframes:

tree: Animates the trees to move from the right side of the screen (1350px) to the left side (-50px)
tree2: Similar to tree, but starts at 650px, creating variation in the scrolling speed and position of the second tree
tree3: Another variation for the third tree, starting at 2750px
rock: Animates rocks from right to left
truck: Simulates the truck's motion by slightly bouncing it up and down to mimic driving over uneven terrain
wind: Animates the "wind" effect on the truck's visual elements by slightly moving them up and down
mtn: Moves the mountain backdrop horizontally from its initial position to create the illusion of distant background movement
hill: Similar to mtn, moves the hill horizontally to simulate scrolling terrain closer to the foreground
```html
 <div class="loop-wrapper">
  <div class="mountain"></div>
  <div class="hill"></div>
  <div class="tree"></div>
  <div class="tree"></div>
  <div class="tree"></div>
  <div class="rock"></div>
  <div class="truck"></div>
  <div class="wheels"></div>
</div> 
```
```css
body { 
  background: #009688;
  overflow: hidden;
  font-family: 'Open Sans', sans-serif;
}
.loop-wrapper {
  margin: 0 auto;
  position: relative;
  display: block;
  width: 600px;
  height: 250px;
  overflow: hidden;
  border-bottom: 3px solid #fff;
  color: #fff;
}
.mountain {
  position: absolute;
  right: -900px;
  bottom: -20px;
  width: 2px;
  height: 2px;
  box-shadow: 
    0 0 0 50px #4DB6AC,
    60px 50px 0 70px #4DB6AC,
    90px 90px 0 50px #4DB6AC,
    250px 250px 0 50px #4DB6AC,
    290px 320px 0 50px #4DB6AC,
    320px 400px 0 50px #4DB6AC
    ;
  transform: rotate(130deg);
  animation: mtn 20s linear infinite;
}
.hill {
  position: absolute;
  right: -900px;
  bottom: -50px;
  width: 400px;
  border-radius: 50%;
  height: 20px;
  box-shadow: 
    0 0 0 50px #4DB6AC,
    -20px 0 0 20px #4DB6AC,
    -90px 0 0 50px #4DB6AC,
    250px 0 0 50px #4DB6AC,
    290px 0 0 50px #4DB6AC,
    620px 0 0 50px #4DB6AC;
  animation: hill 4s 2s linear infinite;
}
.tree, .tree:nth-child(2), .tree:nth-child(3) {
  position: absolute;
  height: 100px; 
  width: 35px;
  bottom: 0;
  background: url(https://s3-us-west-2.amazonaws.com/s.cdpn.io/130015/tree.svg) no-repeat;
}
.rock {
  margin-top: -17%;
  height: 2%; 
  width: 2%;
  bottom: -2px;
  border-radius: 20px;
  position: absolute;
  background: #ddd;
}
.truck, .wheels {
  transition: all ease;
  width: 85px;
  margin-right: -60px;
  bottom: 0px;
  right: 50%;
  position: absolute;
  background: #eee;
}
.truck {
  background: url(https://s3-us-west-2.amazonaws.com/s.cdpn.io/130015/truck.svg) no-repeat;
  background-size: contain;
  height: 60px;
}
.truck:before {
  content: " ";
  position: absolute;
  width: 25px;
  box-shadow:
    -30px 28px 0 1.5px #fff,
     -35px 18px 0 1.5px #fff;
}
.wheels {
  background: url(https://s3-us-west-2.amazonaws.com/s.cdpn.io/130015/wheels.svg) no-repeat;
  height: 15px;
  margin-bottom: 0;
}

.tree  { animation: tree 3s 0.000s linear infinite; }
.tree:nth-child(2)  { animation: tree2 2s 0.150s linear infinite; }
.tree:nth-child(3)  { animation: tree3 8s 0.050s linear infinite; }
.rock  { animation: rock 4s   -0.530s linear infinite; }
.truck  { animation: truck 4s   0.080s ease infinite; }
.wheels  { animation: truck 4s   0.001s ease infinite; }
.truck:before { animation: wind 1.5s   0.000s ease infinite; }


@keyframes tree {
  0%   { transform: translate(1350px); }
  50% {}
  100% { transform: translate(-50px); }
}
@keyframes tree2 {
  0%   { transform: translate(650px); }
  50% {}
  100% { transform: translate(-50px); }
}
@keyframes tree3 {
  0%   { transform: translate(2750px); }
  50% {}
  100% { transform: translate(-50px); }
}

@keyframes rock {
  0%   { right: -200px; }
  100% { right: 2000px; }
}
@keyframes truck {
  0%   { }
  6%   { transform: translateY(0px); }
  7%   { transform: translateY(-6px); }
  9%   { transform: translateY(0px); }
  10%   { transform: translateY(-1px); }
  11%   { transform: translateY(0px); }
  100%   { }
}
@keyframes wind {
  0%   {  }
  50%   { transform: translateY(3px) }
  100%   { }
}
@keyframes mtn {
  100% {
    transform: translateX(-2000px) rotate(130deg);
  }
}
@keyframes hill {
  100% {
    transform: translateX(-2000px);
  }
}





```

===

Menu animations


===


25. Dropdown menus

Dropdown Menus

Loading interactive CodePen...

Open on CodePen
Kevin’s dropdown menus effect creates a dropdown menu with four categories: animals, names, things, and movies. Each category is styled as a button with hover effects that reveal a list of items.
```html
<ul class="hList">
  <li>
    <a href="#click" class="menu">
      <h2 class="menu-title">animals</h2>
      <ul class="menu-dropdown">
        <li>cat</li>
        <li>dog</li>
        <li>horse</li>
        <li>cow</li>
        <li>pig</li>
      </ul>
    </a>
  </li>
  <li>
    <a href="#click" class="menu">
      <h2 class="menu-title menu-title_2nd">names</h2>
      <ul class="menu-dropdown">
        <li>Kevin</li>
        <li>Jim</li>
        <li>Andy</li>
      </ul>
    </a>
  </li>
  <li>
    <a href="#click" class="menu">
      <h2 class="menu-title menu-title_3rd">things</h2>
      <ul class="menu-dropdown">
        <li>bench</li>
        <li>pizza</li>
        <li>Honda CB125</li>
        <li>space</li>
        <li>black matter</li>
        <li>apple</li>
        <li>philodendron</li>
        <li>liver</li>
        <li>brass</li>
      </ul>
    </a>
  </li>
  <li>
    <a href="#click" class="menu">
      <h2 class="menu-title menu-title_4th">Movies</h2>
      <ul class="menu-dropdown">
        <li>Godzilla</li>
        <li>Man on Wire</li>
        <li>Spirited Away</li>
        <li>Interstellar</li>
      </ul>
    </a>
  </li>
</ul>  

```
```scss
@import url(https://fonts.googleapis.com/css?family=Roboto:400,700);

html {
  height: 100%;
  background-color: #f8f8f8;
}

body {
  overflow: hidden;
  height: 100%;
  width: 600px;
  margin: 0 auto;
  background-color: #ffffff;
  font-family: 'Roboto', sans-serif;
  color: #555555;
}

a {
  text-decoration: none;
  color: inherit;
}

* {
  box-sizing: border-box;
}

$menu_WIDTH: 150px;

.menu {
  display: block;
  position: relative;
  cursor: pointer;
}

.menu-title {
  display: block;
  width: $menu_WIDTH;
  height: 40px;
  padding: 12px 0 0;
  background: #9dc852;
  text-align: center;
  color: #ffffff;
  font-weight: bold;
  text-transform: uppercase;
  transition: 0.3s background-color;
}

.menu-title:before {
  content: "";
  display: block;
  height: 0;
  border-top: 5px solid #9dc852;
  border-left: ($menu_WIDTH / 2) solid transparent;
  border-right: ($menu_WIDTH / 2) solid transparent;
  border-bottom: 0 solid #dddddd;
  position: absolute;
  top: 100%;
  left: 0;
  z-index: 101;
  transition:
    0.2s 0.2s border-top ease-out,
    0.3s border-top-color;
}

.menu-title:hover { background: #8db842; }
.menu-title:hover:before { border-top-color: #8db842; }

.menu:hover > .menu-title:before {
  border-top-width: 0;
  transition:
    0.2s border-top-width ease-in,
    0.3s border-top-color;
}

.menu-title:after {
  content: "";
  display: block;
  height: 0;
  border-left: ($menu_WIDTH / 2) solid transparent;
  border-right: ($menu_WIDTH / 2) solid transparent;
  border-bottom: 0 solid #ebebeb;
  position: absolute;
  bottom: 0;
  left: 0;
  z-index: 101;
  transition: 0.2s border-bottom ease-in;
}

.menu:hover > .menu-title:after {
  border-bottom-width: 5px;
  transition: 0.2s 0.2s border-bottom-width ease-out;
}

.menu-title_2nd { background: #4e96b3; }
.menu-title_2nd:hover { background: #3e86a3; }
.menu-title_2nd:before { border-top-color: #4e96b3; }
.menu-title_2nd:hover:before { border-top-color: #3e86a3; }

.menu-title_3rd { background: #c97676; }
.menu-title_3rd:hover { background: #b96666; }
.menu-title_3rd:before { border-top-color: #c97676; }
.menu-title_3rd:hover:before { border-top-color: #b96666; }

.menu-title_4th { background: #dbab58; }
.menu-title_4th:hover { background: #cb9b48; }
.menu-title_4th:before { border-top-color: #dbab58; }
.menu-title_4th:hover:before { border-top-color: #cb9b48; }

.menu-dropdown {
  min-width: 100%;
  padding: 15px 0;
  position: absolute;
  background: #ebebeb;
  z-index: 100;
  transition:
    0.5s padding,
    0.5s background;
}

.menu-dropdown:after {
  content: "";
  display: block;
  height: 0;
  border-top: 5px solid #ebebeb;
  border-left: ($menu_WIDTH / 2) solid transparent;
  border-right: ($menu_WIDTH / 2) solid transparent;
  position: absolute;
  top: 100%;
  left: 0;
  z-index: 101;
  transition: 0.5s border-top;
}

.menu:not(:hover) > .menu-dropdown {
  padding: 4px 0;
  background: #dddddd;
  z-index: 99;
}

.menu:not(:hover) > .menu-dropdown:after {
  border-top-color: #dddddd;
}

.menu:not(:hover) > .menu-title:after {
  border-bottom-color: #dddddd;
}

.menu-dropdown > * {
  overflow: hidden;
  height: 30px;
  padding: 5px 10px;
  background: rgba(0,0,0,0);
  white-space: nowrap;
  transition: 
    0.5s height cubic-bezier(.73,.32,.34,1.5),
    0.5s padding cubic-bezier(.73,.32,.34,1.5),
    0.5s margin cubic-bezier(.73,.32,.34,1.5),
    0.5s 0.2s color,
    0.2s background-color;
}

.menu-dropdown > *:hover {
  background: rgba(0,0,0,0.1);
}

.menu:not(:hover) > .menu-dropdown > * {
  visibility: hidden;
  height: 0;
  padding-top: 0;
  padding-bottom: 0;
  margin: 0;
  color: rgba(25,25,25,0);
  transition: 
    0.5s 0.1s height,
    0.5s 0.1s padding,
    0.5s 0.1s margin,
    0.3s color,
    0.6s visibility;
  z-index: 99;
}

.hList {
  //overflow: hidden;
}

.hList > * {
  float: left;
}

.hList > * + * {
  margin-left: 0;
}
```
===


26. Swanky CSS Drop Down menu

Swanky Pure CSS Drop Down Menu V2.0


The swanky CSS Drop Down menu by Jamie Coulter provides a combination of click and hover animations that will make any menu stand out. When the menu links are clicked, the sub-menus stagger into view one after the other. The social media buttons aren’t left out, as a slide animation is applied to its content on hover.
```haml
// My brand

.brand
  %a{:href => 'https://www.jamiecoulter.co.uk',:target => '_blank'}
    %img{:src => 'https://s3-us-west-2.amazonaws.com/s.cdpn.io/217233/logo.png'}

// Begin Body

.swanky

  // Introduction Block
  
  .swanky_title
    %h1 Swanky Lil Drop Down Menu V2.0
    %p Pure CSS Drop down menu. Nice little addition to any non-javascript user interface. Uses the labels for trick to toggle animations.
    .swanky_title__social
      %a{:href => 'https://www.twitter.com/jamiecoulter89',:target => '_blank'}
        .slide
          .arrow
            .stem
            .point
        %img{:src => 'https://s3-us-west-2.amazonaws.com/s.cdpn.io/217233/tw.png'}
        Twitter
    .swanky_title__social
      %a{:href => 'https://www.codepen.io/jcoulterdesign/',:target => '_blank'}
        .slide
          .arrow
            .stem
            .point
        %img{:src => 'https://s3-us-west-2.amazonaws.com/s.cdpn.io/217233/cp.png'}
        Codepen 
        
  //////////// Begin Dropdown ////////////
  
  .swanky_wrapper
    %input{:type => 'radio',:name => 'radio',:id => 'Dashboard'}
    %label{:for => 'Dashboard'}
      %img{:src => 'https://s3-us-west-2.amazonaws.com/s.cdpn.io/217233/dash.png'}
      %span Dashboard
      %div.lil_arrow
      %div.bar
      .swanky_wrapper__content
        %ul
          %li Tools
          %li Reports
          %li Analytics
          %li Code Blocks
    %input{:type => 'radio',:name => 'radio',:id => 'Sales'}
    %label{:for => 'Sales'}
      %img{:src => 'https://s3-us-west-2.amazonaws.com/s.cdpn.io/217233/del.png'}
      %span Sales
      %div.lil_arrow
      %div.bar
      .swanky_wrapper__content
        %ul
          %li New Sales
          %li Expired Sales
          %li Sales Reports
          %li Deliveries
    %input{:type => 'radio',:name => 'radio',:id => 'Messages'}
    %label{:for => 'Messages'}
      %img{:src => 'https://s3-us-west-2.amazonaws.com/s.cdpn.io/217233/mess.png'}
      %span Messages
      %div.lil_arrow
      %div.bar
      .swanky_wrapper__content
        %ul
          %li Inbox
          %li Outbox
          %li Sent
          %li Archived
    %input{:type => 'radio',:name => 'radio',:id => 'Users'}
    %label{:for => 'Users'}
      %img{:src => 'https://s3-us-west-2.amazonaws.com/s.cdpn.io/217233/users.png'}
      %span Users
      %div.lil_arrow
      %div.bar
      .swanky_wrapper__content
        %ul
          %li New User
          %li User Groups
          %li Permissions
          %li Passwords
    %input{:type => 'radio',:radio => 'radio',:id => 'Settings'}
    %label{:for => 'Settings'}
      %img{:src => 'https://s3-us-west-2.amazonaws.com/s.cdpn.io/217233/set.png'}
      %span Settings
      %div.lil_arrow
      .swanky_wrapper__content
        %ul
          %li Databases
          %li Design
          %li Change User
          %li Log Out
          
  //////////// End Dropdown ////////////
  
// My Footer

.love
  %p Made with <img src="https://s3-us-west-2.amazonaws.com/s.cdpn.io/217233/love_copy.png" /> by <a href='https://www.jamiecoulter.co.uk' target='_blank'> Jcoulterdesign </a>
```
```css
// Import Fonts

@import url(https://fonts.googleapis.com/css?family=Roboto:400,700,300); // Roboto

// Variables

$default-font:'Roboto', sans-serif; // Roboto Google font
$global-font-size:12px; // Global font sizing
$global-font-weight:500; // Global font weight
$global-font-smoothing:antialiased; // Global smoothing method
$content-width:700px; // Width of wrapper
$background:linear-gradient(135deg, #8254EA 0%, #E86DEC 100%); // Body background
$full-height: 100vh; // Height of body

$title-width:400px; // Width of title block

// Reset

ul{padding:0;margin:0;}li{list-style-type:none;}input[type='radio']{display:none;}label{cursor:pointer}::-webkit-scrollbar {display: none; }

// Placeholders

%center{
     margin:auto;
    top:0;
    bottom:0;
    left:0;
    right:0;
}

// Styles

body{
  height: $full-height;
  font-weight: $global-font-weight;
  font-family:$default-font;
  background:$background;
  -webkit-font-smoothing: $global-font-smoothing;
  font-size: $global-font-size;
  .swanky{
    @extend %center;
    perspective:600px;
    width:$content-width;
    position:absolute;
    margin:auto;
    height:360px;
    &_title{
      float:right;
      text-align:left;
      width:$title-width;
      color:white;
      position:relative;
      top:70px;
      &__social a{
        position:relative;
        overflow:hidden;
        .slide{
          height: 45px;
          width: 100px;
          float: left;
          position: absolute;
          transform: skew(20deg);
          left: -120px;
          transition-property:left;
          transition-duration:.2s;
          background: white;
          .arrow{
            position: absolute;
            right: 31px;
            top: 24px;
            opacity:0;
            transform: skew(-20deg);
            .stem{
              width: 10px;
              height: 2px;
              background: rgb(133, 132, 144);
            }
            .point{
              width: 6px;
              height: 6px;
              border-right: 2px solid rgb(133, 132, 144);
              top: -3px;
              right: 1px;
              position: absolute;
              transform: rotate(45deg);
              border-top: 2px solid rgb(133, 132, 144);
            }
          }
        }
        width: 140px;
        margin-right: 15px;
        text-decoration:none;
        padding: 0px 0px 5px 0px;
        height: 40px;
        border: 2px solid white;
        float: left;
        color: white;
        font-size: 12px;
        font-weight: 400;
        margin-top:20px;
        img{
          width: 16px;
          margin-left: 10px;
          position: relative;
          margin-right: 8px;
          transition-property:margin-left;
          transition-duration:.1s;
          margin-top: 10px;
          top: 4px;
        }
        &:hover > .slide{
          left:-70px;
          transition-property:left;
          transition-duration:.1s;
        }
        &:hover > img{
          margin-left:40px;
          transition-property:margin-left;
          transition-duration:.1s;
        }
        &:hover > .slide .arrow{
          right:11px;
          opacity:1;
          transition-property:right,opacity;
          transition-delay:.07s;
          transition-duration:.2s;
        }
      }
    }
    .intro{
      float:right;
      color:white;
      width:370px;
      top:50px;
      position:relative;
      h1{
        text-shadow: 0px 2px rgba(0, 0, 0, 0.26);
      }
      p{
        line-height:20px;
        text-shadow: 0px 1px rgba(0, 0, 0, 0.26);
      }
    }
    &_wrapper{
      width: 225px;
      //transform: rotateY(14deg) rotateX(-2deg) rotateZ(-2deg);
      height: auto;
      overflow: hidden;
      border-radius: 4px;
      background: #2a394f;
      label{
        padding:25px;
        float:left;
        height:72px;
        border-bottom: 1px solid #293649;
        position:relative;
        width:100%;
        color:rgb(239, 244, 250);
        transition:text-indent .15s, height .3s;
        box-sizing:border-box;
        img{
          margin-right:10px;
          position:relative;
          top: 2px;
          width:16px;
        }
        span{
          position :relative;
          top:-3px
        }
        &:hover{
          background: rgb(33, 46, 65);
          border-bottom: 1px solid #2A394F;
          text-indent:4px;
        }
        &:hover .bar{
          width:100%;
        }
        .bar{
          width: 0px;
          transition:width .15s;
          height: 2px;
          position: absolute;
          display: block;
          background: rgb(53, 87, 137);
          bottom: 0;
          left: 0;
        }
        .lil_arrow{
          width:5px;
          height:5px;
          -webkit-transition: transform 0.8s;
          transition: transform 0.8s;
          -webkit-transition-timing-function: cubic-bezier(0.68, -0.55, 0.265, 1.55);
          border-top:2px solid white;
          border-right:2px solid white;
          float:right;
          position:relative;
          top: 6px;
          right: 2px;
          transform:rotate(45deg)
        }
      }
      &__content{
        position: absolute;
        display: none;
        overflow: hidden;
        left: 0;
        width: 100%;
        li{
          width:100%;
          opacity:0;
          left:-100%;
          background: #15a4fa;
          padding:25px 0px;
          text-indent:25px;
          box-shadow: 0px 0px #126CA1  inset;
            transition:box-shadow .3s,text-indent .3s;
          position:relative;
          &:hover{
            background:#0c93e4;
            box-shadow: 3px 0px #126CA1  inset;
            transition:box-shadow .3s linear,text-indent .3s linear;
            text-indent:31px;

          }
        }
        .clear{
          clear:both;
        }
      }
    }
  }
}

// Hide show content

input[type='radio']:checked + label .swanky_wrapper__content{
  display: block;
  top: 68px;
  border-bottom: 1px solid rgb(33, 46, 65);
}
input[type="radio"]:checked + label > .lil_arrow {
  -webkit-transition: -webkit-transform 0.8s;
  transition: transform 0.8s;
  -webkit-transition-timing-function: cubic-bezier(0.68, -0.55, 0.265, 1.55);
  -webkit-transform: rotate(135deg);
  -ms-transform: rotate(135deg);
  transform: rotate(135deg);
  border-top: 2px solid rgb(20, 163, 249);
  border-right: 2px solid rgb(20, 163, 249);
}
input[type='radio']:checked + label{
  height: 325px;background: #212e41;
  text-indent:4px;
  transition-property:height;
  transition-duration:.6s; -webkit-transition-timing-function: cubic-bezier(0.68, -0.55, 0.265, 1.55);
}
input[type='radio']:checked + label .bar{
  width:0;
}

input[type='radio']:checked + label{
  @for $i from 1 through 4{
    li:nth-of-type(#{$i}){
      animation:in .15s .45s + $i/8 forwards;
      -webkit-transition-timing-function: cubic-bezier(0.68, -0.55, 0.265, 1.55);
      -moz-animation:in .15s .45s + $i/8 forwards;
      -moz-transition-timing-function: cubic-bezier(0.68, -0.55, 0.265, 1.55);
    }
  } 
}

// Animations

@keyframes in{
  from{left:-100%;opacity:0}
  to{left:0;opacity:1}
}

// My Styles

.love{
  position: absolute;
  right: 20px;
  bottom: 0px;
  font-size: 11px;
  font-weight: normal;
  p{
    color:white;
    font-weight:normal;
    font-family: 'Open Sans', sans-serif;
  }
  a{
    color:white;
    font-weight:700;
    text-decoration:none;
  }
  img{
    position:relative;
    top:3px;
    margin:0px 4px;
    width:10px;
  }
}
.brand{
  position:absolute;
  left:20px;
  bottom:14px;
  img{
    width:30px;
  }
}
```
===


27. Pure CSS menu
Pure CSS menu


The pure CSS menu by Antonija Šimić creates a fullscreen menu design that features a toggle button that:

Transforms into an animated "X" when clicked
Opens two animated sections: a left panel with a call-to-action (CTA) button and a right panel with navigational links.
The left panel slides down vertically, while the right panel slides in horizontally, creating a dynamic opening effect. The menu links include a hover animation where an underline shifts from left to right for enhanced interactivity.
```html
<main>
  <h1>
    Fullscreen menu
    <span> with cool links </span>
  </h1>
  <input type="checkbox" id="myInput">
  <label for="myInput">
      <span class="bar top"></span>
      <span class="bar middle"></span>
      <span class="bar bottom"></span>
    </label>
  <aside>
    <div class="aside-section aside-left">
      <div class="aside-content">
        <p> Some text that will make you click the cta </p>
        <button class="button"> CTA </button>
      </div>
    </div>
    <div class="aside-section aside-right">
      <ul class="aside-list">
        <li><a href="" class="aside-anchor">Link</a></li>
        <li><a href="" class="aside-anchor">Link</a></li>
        <li><a href="" class="aside-anchor">Link</a></li>
        <li><a href="" class="aside-anchor">Link</a></li>
      </ul>
    </div>
  </aside>
</main>
```
```css
HTML CSSResult Skip Results Iframe
@import url('https://fonts.googleapis.com/css?family=Montserrat');

body,
html {
  background-color: #fff;
  height: 100%;
  width: 100%;
  padding: 0;
  margin: 0;
  font-family: "Montserrat", sans-serif;
}

main {
  height: 100%;
  position: relative;
  overflow: hidden;
}

.aside-section {
  top: 0;
  bottom: 0;
  position: absolute;
}

.aside-left {
  display: none;
  width: 40%;
  left: 0;
  background-color: #ff5964;
  -webkit-transform: translateY(-100%);
  -moz-transform: translateY(-100%);
  -ms-transform: translateY(-100%);
  -o-transform: translateY(-100%);
  transform: translateY(-100%);
  transition: transform 0.4s ease-in-out;
}

.aside-right {
  width: 100%;
  right: 0;
  background-color: #38618c;
  -webkit-transform: translateX(100%);
  -moz-transform: translateX(100%);
  -ms-transform: translateX(100%);
  -o-transform: translateX(100%);
  transform: translateX(100%);
  transition: transform 0.4s ease-in-out;
}

.aside-list {
  list-style: none;
  padding: 0;
  margin: 0;
  margin-top: 150px;
  text-align: left;
  padding-left: 50px;
}

.aside-content {
  margin-top: 150px;
  padding: 0 40px;
  position: relative;
  color: white;
  text-align: center;
}

.aside-list li {
  margin-bottom: 20px;
}

.aside-anchor::after {
  content: "";
  position: absolute;
  bottom: 0;
  background-color: #ff5964;
  left: 0;
  right: 0;
  height: 3px;
  border-radius: 3px;
}

.aside-anchor::before {
  border-radius: 3px;
  content: "";
  position: absolute;
  bottom: 0;
  background-color: #fff;
  left: 0;
  height: 3px;
  z-index: 1;
  width: 50%;
  -webkit-transition: transform 0.2s ease-in-out;
  -o-transition: transform 0.2s ease-in-out;
  transition: transform 0.2s ease-in-out;
}

.aside-anchor:hover:before {
  -webkit-transform: translateX(100%);
  -moz-transform: translateX(100%);
  -ms-transform: translateX(100%);
  -o-transform: translateX(100%);
  transform: translateX(100%);
}

.aside-anchor {
  padding-bottom: 7px;
  color: #fff;
  text-decoration: none;
  font-size: 30px;
  position: relative;
  font-weight: 500;
}

input[type="checkbox"] {
  display: none;
}

input[type="checkbox"]:checked ~ aside .aside-left {
  transform: translateY(0%);
}

input[type="checkbox"]:checked ~ aside .aside-right {
  transform: translateX(0%);
}

input[type="checkbox"]:checked ~ label .bar {
  background-color: #fff;
}

input[type="checkbox"]:checked ~ label .top {
  -webkit-transform: translateY(0px) rotateZ(45deg);
  -moz-transform: translateY(0px) rotateZ(45deg);
  -ms-transform: translateY(0px) rotateZ(45deg);
  -o-transform: translateY(0px) rotateZ(45deg);
  transform: translateY(0px) rotateZ(45deg);
}

input[type="checkbox"]:checked ~ label .bottom {
  -webkit-transform: translateY(-15px) rotateZ(-45deg);
  -moz-transform: translateY(-15px) rotateZ(-45deg);
  -ms-transform: translateY(-15px) rotateZ(-45deg);
  -o-transform: translateY(-15px) rotateZ(-45deg);
  transform: translateY(-15px) rotateZ(-45deg);
}

input[type="checkbox"]:checked ~ label .middle {
  width: 0;
}

.middle {
  margin: 0 auto;
}

label {
  top: 10px;
  display: inline-block;
  padding: 7px 10px;
  background-color: transparent;
  cursor: pointer;
  margin: 10px;
  z-index: 3;
  position: fixed;
}

.bar {
  display: block;
  background-color: #38618c;
  width: 30px;
  height: 3px;
  border-radius: 5px;
  margin: 5px auto;
  transition: background-color 0.4s ease-in, transform 0.4s ease-in,
    width 0.4s ease-in;
}

h1 {
  margin: 0;
  position: relative;
  top: 50%;
  left: 0;
  right: 0;
  transform: translateY(-50%);
  text-align: center;
  font-size: 30px;
}

h1 span {
  font-size: 20px;
  display: block;
}

p {
  font-size: 30px;
}

.button {
  display: inline-block;
  background-image: none;
  border: none;
  background-color: transparent;
  padding-bottom: 7px;
  position: relative;
  cursor: pointer;
  font-size: 20px;
  color: white;
  padding: 7px 50px;
  border: 2px solid white;
}

@media (min-width: 992px) {
  h1 {
    font-size: 40px;
  }
  .aside-left {
    display: block;
  }

  .aside-right {
    width: 60%;
  }
}



Resources1× 0.5× 0.25×Rerun
```
===


28. 💪 CSS menu feat. emoji

💪 CSS menu feat. emoji

Loading interactive CodePen...

Open on CodePen
💪 CSS menu feat. emoji by Piotr Galor defines a navigation menu with skewered sub-menus that appear when hovered over. It is a simple but catchy menu animation.
```html
<nav class="menu">
  <ol>
    <li class="menu-item"><a href="#0">Home</a></li>
    <li class="menu-item"><a href="#0">About</a></li>
    <li class="menu-item">
      <a href="#0">Widgets</a>
      <ol class="sub-menu">
        <li class="menu-item"><a href="#0">Big Widgets</a></li>
        <li class="menu-item"><a href="#0">Bigger Widgets</a></li>
        <li class="menu-item"><a href="#0">Huge Widgets</a></li>
      </ol>
    </li>
    <li class="menu-item">
      <a href="#0">Kabobs</a>
      <ol class="sub-menu">
        <li class="menu-item"><a href="#0">Shishkabobs</a></li>
        <li class="menu-item"><a href="#0">BBQ kabobs</a></li>
        <li class="menu-item"><a href="#0">Summer kabobs</a></li>
      </ol>
    </li>
    <li class="menu-item"><a href="#0">Contact</a></li>
  </ol>
</nav>
```
```css
@import url('https://fonts.googleapis.com/css?family=Exo+2');

*,
*:before,
*:after{
  box-sizing: border-box;
  -webkit-tap-highlight-color: rgba(255,255,255,0);
}

body{
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100vw;
  height: 100vh;
  margin: 0;
  padding: 0;
  font-family: 'Exo 2';
  background: #131313;
  font-size: 12px;
  overflow-x: hidden;
} 

a{
  text-decoration: none;
  color: #fff;
  padding: 13px;
  opacity: .8;
  white-space: nowrap;
}

a:hover{
  opacity: 1;
}

a:before{
  font-family: apple color emoji,segoe ui emoji,notocoloremoji,segoe ui symbol,android emoji,emojisymbols,emojione mozilla;
}

.menu-item:first-of-type a{
  border-radius: 13px 0 0 21px;
}

.menu-item:last-of-type a{
  border-radius: 0 21px 13px 0;
}

.sub-menu .menu-item:first-of-type a{
  border-radius: 13px 21px 0 0;
}

.sub-menu .menu-item:last-of-type a{
  border-radius: 0 0 21px 13px;
}
 
nav{
  max-width: 360px;
  max-height: 480px;
  display: flex;
  align-items: flex-end;
  animation: bounceIn 300ms cubic-bezier(0.175, 0.885, 0.320, 1.275) 1 forwards;
  animation-delay: 500ms;
  transform-style: preserve-3d;
  opacity: 0;
}

@keyframes bounceIn{
  0%{
    opacity: 0;
    transform: scaleX(.55) scaleY(.89);
  }
  50%{
    opacity: 1;   
  }
  100%{
    opacity: 1;   
  }
}

 ol{
  list-style: none;
  padding: 0;
  margin: 0;
}

.sub-menu{
  opacity: 0;
  pointer-events: none;
  position: absolute;
  bottom: 89px;
  transform: skewY(-5deg) scale(.89) rotateX(-5deg) rotateZ(-3deg);
}

.sub-menu a{
    font-size: 15px;
}

.menu-item{
  position: relative;
  display: inline-flex;
}

.menu-item:hover .sub-menu{
  pointer-events: all;
  animation: showBounce 300ms cubic-bezier(0.175, 0.885, 0.320, 1.275) forwards;
  transform-style: preserve-3d;
}

@keyframes showBounce{
  100%{
    opacity: 1;   
    transform: translateX(-21px) skewY(-5deg);
  }
}


.sub-menu li{
  display: flex;
}

.sub-menu a{
  font-size: 17px;
}


.menu-item:nth-of-type(3) .sub-menu li:nth-of-type(2) a{
  font-size: 21px;
}

.menu-item:nth-of-type(3) .sub-menu li:nth-of-type(3) a{
  font-size: 27px;
}

.menu-item a{
  position: relative;
  text-align: center;
}

.menu-item a:before{
  content: '';
  display: block;
  font-size: 34px;
  transform: rotateZ(-8deg);
  margin-bottom: 5px;
  transition: transform 189ms ease-out;
}

.menu-item a:hover:before{
  animation: hoverEmoji 600ms cubic-bezier(0.175, 0.885, 0.320, 1.275) forwards;
}

@keyframes hoverEmoji{
  0%{
    transform: scaleX(.89) rotateX(-21deg) rotateZ(-8deg);
  }
  100%{
  transform:  rotateZ(-8deg);
  }
}

nav > ol > .menu-item:nth-of-type(1) > a:before{
  content: '🏠';
}

nav > ol > .menu-item:nth-of-type(2) > a:before{
  content: '🤷';
}

nav > ol > .menu-item:nth-of-type(3) > a:before{
  content: '🛠️';
}

nav > ol > .menu-item:nth-of-type(4) > a:before{
  content: '🍽️';
}

nav > ol > .menu-item:nth-of-type(5) > a:before{
  content: '📨';
}

nav > ol > .menu-item:nth-of-type(3) ol li a:before,
nav > ol > .menu-item:nth-of-type(4) ol li a:before{
  content: '💪';
  display: inline-flex;
  font-size: inherit;
  transform: rotateZ(-8deg);
  margin-right: 5px;
  transition: transform 300ms ease-out;
}

nav > ol > li{
  width: 20%;
  margin-right: -4px;
}

nav > ol > .menu-item:nth-of-type(4) ol li:nth-of-type(1) a:before{
  content: '🍖';
}

nav > ol > .menu-item:nth-of-type(4) ol li:nth-of-type(2) a:before{
  content: '♨️';
}

nav > ol > .menu-item:nth-of-type(4) ol li:nth-of-type(3) a:before{
  content: '☀️';
}


```
===


29. Pure CSS menu animation

Pure CSS Menu Animation

Loading interactive CodePen...

Open on CodePen
The pure CSS menu animation by Charlie Marcotte creates a menu with a white bottom panel containing three hoverable headings — Explore, Connect, and Interact. This effect also includes a pulsating blob-like element that shifts position based on which heading is being hovered.

First time here? Discover what Prismic can do!
Build locally with your preferred tech stack and spend less time fixing, and more time shipping.


```pug
.menu-container
  input(type="checkbox" id="menu")
  label(for="menu")
    .menu
  .box
    h1 Explore
    h1 Connect
    h1 Interact
    .move-item
```
```scss
$primary: #333;
@import url('https://fonts.googleapis.com/css?family=Montserrat');

@mixin psuedo {
  content: '';
  position: absolute;
  background: $primary;
  width: 55px;
  height: 3px;
  transition: .5s;
  transition-delay: .2s;
}

$colors: (
  1: #DEF06C,
  2: #71C1D1,
  3: #DD7171
);

body {
  background: #ddd;
  overflow: hidden;
  font-family: 'Montserrat', sans-serif;
}

.menu-container {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100vh;
  width: 100vw;
}

label {
  position: relative;
  display: flex;
  justify-content: center;
  align-items: center;
  height: 50px;
  width: 65px;
  cursor: pointer;
  transition: .5s;
  transition-delay: .7s;
}

#menu {
  display: none;
}

#menu:checked ~ label {
  transform: translateY(-100px);
}

.menu {
  position: relative;
  width: 55px;
  height: 3px;
  background: $primary;
}

.menu:after, .menu:before {
  @include psuedo;
}

.menu:after {
  top: -10px;
}

.menu:before {
  top: 10px;
}

#menu:checked ~ label .menu:after {
  transform: translateY(10px);           opacity: 0;
}

#menu:checked ~ label .menu:before {
  transform: translateY(-10px);
  opacity: 0;
}

#menu:checked ~ label .menu {
  animation: move .5s ease forwards 1;
  animation-delay: .6s;
  @keyframes move {
    100% {
      transform: rotate(45deg);
    }
  }
}

#menu:checked ~ label .menu:before {
  animation: move1 .5s ease forwards 1;
  animation-delay: .6s;
  @keyframes move1 {
    0% {
      opacity: 1;
    }
    100% {
      transform: rotate(90deg) translateY(2px) translateX(-8px);
      opacity: 1;
      height: 3px;
    }
  }
}

.box {
  position: absolute;
  background: white;
  bottom: 0px;
  left: 0px;
  width: 100%;
  height: 300px;
  transition: .8s;
  transition-delay: .7s;
  overflow: hidden;
  transform: translateY(300px);
  display: flex;
  justify-content: center;
  align-items: center;
}

#menu:checked ~ .box {
  transform: translateY(0px);
}

h1 {
  position: relative;
  font-size: 40px;
  margin-right: 100px;
  cursor: pointer;
  &:last-child {
    margin-right: 0px;
  }
  &:first-child {
    margin-left: 115px;
  }
}

.move-item {
  position: absolute;
  height: 350px;
  width: 350px;
  background: #ddd;
  border-top-right-radius: 400px;
  border-bottom-right-radius: 125px;
  top: -270px;
  transition: .7s;
  transition-delay: .1s;
  transform: translateX(-250px) rotate(45deg);
  animation: ooz 3s linear infinite;
  @keyframes ooz {
    0% {
    }
    50% {
      border-bottom-left-radius: 300px;
      border-top-right-radius: 0px;
    }
  }
}

@for $x from 1 through 3 {
  h1:nth-child(#{$x}):after {
    position: absolute;
    content: '';
    bottom: 0px;
    left: 0px;
    width: 100%;
    height: 4px;
    background: map-get($colors, $x);
    }
}
h1:nth-child(1):hover ~ .move-item {
  transform: translate(-250px) rotate(45deg);
}

h1:nth-child(2):hover ~ .move-item {
  transform: translate(10px) rotate(45deg);
}

h1:nth-child(3):hover ~ .move-item {
  transform: translate(280px) rotate(45deg);
}
```

===
Card animations

===

30. 3D cards flip

3D Cards Flip

Loading interactive CodePen...

Open on CodePen
The 3D cards flip effect by Julien Sulpis creates an interactive 3D card-flipping animation. The .board contains three .card elements, each of which can flip between its front and back faces when clicked. As the cards flip, they stack on top of each other and, when clicked again, revert back to their original state and position.
```html
<article class="board">
  <button class="card" onclick="this.classList.toggle('flipped')">
    <span class="wrapper"><span class="content">
        <span class="face back"></span>
        <span class="face front"></span>
      </span>
    </span>
  </button>
  <button class="card" onclick="this.classList.toggle('flipped')">
    <span class="wrapper"><span class="content">
        <span class="face back"></span>
        <span class="face front"></span>
      </span>
    </span>
  </button>
  <button class="card" onclick="this.classList.toggle('flipped')">
    <span class="wrapper"><span class="content">
        <span class="face back"></span>
        <span class="face front"></span>
      </span>
    </span>
  </button>
</article>
```
```scss
$card-space: 3px;

html,
body {
  height: 100%;
}

body {
  margin: 0;
  display: grid;
  place-items: center;
  perspective: 1000px;
}

.board {
  --card-width: min(200px, 20vmin);
  display: flex;
  align-items: center;
  width: calc(4 * var(--card-width));
  box-sizing: content-box;
  aspect-ratio: 16/9;
  transform: rotateX(60deg);
  border: 4px solid black;
  border-radius: 16px;
  padding: 1rem 3rem;
  background: hsl(201deg, 100%, 32%);
}

.board,
.card,
.wrapper,
.content {
  transform-style: preserve-3d;
}

.card {
  --duration: 1200ms;
  position: absolute;
  width: var(--card-width);
  aspect-ratio: 20/29;
  outline: none;
  border: none;
  cursor: pointer;
  padding: 0;
  background-color: transparent;
  transition: all 200ms;
  pointer-events: none;

  @each $i in (1, 2, 3) {
    &:nth-child(#{$i}) {
      transform: translateZ($i * $card-space);
    }
  }
}

.wrapper {
  pointer-events: initial;
  display: block;
  position: relative;
  height: 100%;
  transition: all var(--duration) ease-out;
  transform-origin: 200% 50%;
}

.content {
  display: block;
  height: 100%;
  transition: all var(--duration);
}

.face {
  transition: transform calc(var(--duration) * 3 / 4);
  transition-delay: calc(var(--duration) / 6);
  position: absolute;
  inset: 0;
  backface-visibility: hidden;
  border-radius: calc(var(--card-width) / 20);
  background-size: cover;
  background-position: center;
  background-color: white;
}

.front {
  transform: rotateZ(0.5turn) rotateY(-0.5turn);
  border-width: 1px 0px;
  border-color: black;
  border-style: solid;

  .card:nth-of-type(1) & {
    background-image: url("https://upload.wikimedia.org/wikipedia/commons/thumb/1/19/English_pattern_ace_of_spades.svg/1024px-English_pattern_ace_of_spades.svg.png");
  }
  .card:nth-of-type(2) & {
    background-image: url("https://upload.wikimedia.org/wikipedia/commons/thumb/0/0b/English_pattern_2_of_spades.svg/1024px-English_pattern_2_of_spades.svg.png");
  }
  .card:nth-of-type(3) & {
    background-image: url("https://upload.wikimedia.org/wikipedia/commons/thumb/a/a5/English_pattern_3_of_spades.svg/1024px-
English_pattern_3_of_spades.svg.png");
  }
}

.back {
  background-image: url("https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTkKnlurWcUYrlDbGvf8IUz6rLX7oX1hHt7FW_6e4vNOwFfPhmPURXxGK45qVAqW7dtxsY&usqp=CAU");
  border: 1px solid black;
}

.card.flipped {
  @each $i in (1, 2, 3) {
    &:nth-child(#{$i}) {
      transform: translateZ((4 - $i) * $card-space);
    }
  }

  .wrapper {
    transform: rotateY(0.5turn);
  }

  .content {
    transform: rotateX(-0.5turn) rotateY(1.5turn);
  }
}

```
===


31. Card hover effect

Card hover effect

The card hover effect by Aaron Iker creates cards that, when hovered on, receive a shiny tile effect. Among others, it relies on CSS features like mask-image (docs), backdrop-filter (docs), conic-gradient (docs), radial-gradient (docs), and @keyframes tile, which animates the opacity of the tiles on hover.
```html
<div class="grid">
  <div class="card">
    <span class="icon">
      <svg
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="1.2"
        stroke-linecap="round"
        stroke-linejoin="round"
        xmlns="http://www.w3.org/2000/svg"
      >
        <path
          d="M14.5 3.5C14.5 3.5 14.5 5.5 12 5.5C9.5 5.5 9.5 3.5 9.5 3.5H7.5L4.20711 6.79289C3.81658 7.18342 3.81658 7.81658 4.20711 8.20711L6.5 10.5V20.5H17.5V10.5L19.7929 8.20711C20.1834 7.81658 20.1834 7.18342 19.7929 6.79289L16.5 3.5H14.5Z"
        />
      </svg>
    </span>
    <h4>Products</h4>
    <p>
      Standard chunk of Lorem Ipsum used since the 1500s is showed below
      for those interested.
    </p>
    <div class="shine"></div>
    <div class="background">
      <div class="tiles">
        <div class="tile tile-1"></div>
        <div class="tile tile-2"></div>
        <div class="tile tile-3"></div>
        <div class="tile tile-4"></div>

        <div class="tile tile-5"></div>
        <div class="tile tile-6"></div>
        <div class="tile tile-7"></div>
        <div class="tile tile-8"></div>

        <div class="tile tile-9"></div>
        <div class="tile tile-10"></div>
      </div>

      <div class="line line-1"></div>
      <div class="line line-2"></div>
      <div class="line line-3"></div>
    </div>
  </div>
  <div class="card">
    <span class="icon">
      <svg
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="1.2"
        stroke-linecap="round"
        stroke-linejoin="round"
        xmlns="http://www.w3.org/2000/svg"
      >
        <path
          d="M4.5 9.5V5.5C4.5 4.94772 4.94772 4.5 5.5 4.5H9.5C10.0523 4.5 10.5 4.94772 10.5 5.5V9.5C10.5 10.0523 10.0523 10.5 9.5 10.5H5.5C4.94772 10.5 4.5 10.0523 4.5 9.5Z"
        />
        <path
          d="M13.5 18.5V14.5C13.5 13.9477 13.9477 13.5 14.5 13.5H18.5C19.0523 13.5 19.5 13.9477 19.5 14.5V18.5C19.5 19.0523 19.0523 19.5 18.5 19.5H14.5C13.9477 19.5 13.5 19.0523 13.5 18.5Z"
        />
        <path d="M4.5 19.5L7.5 13.5L10.5 19.5H4.5Z" />
        <path
          d="M16.5 4.5C18.1569 4.5 19.5 5.84315 19.5 7.5C19.5 9.15685 18.1569 10.5 16.5 10.5C14.8431 10.5 13.5 9.15685 13.5 7.5C13.5 5.84315 14.8431 4.5 16.5 4.5Z"
        />
      </svg>
    </span>
    <h4>Categories</h4>
    <p>
      Standard chunk of Lorem Ipsum used since the 1500s is showed below
      for those interested.
    </p>
    <div class="shine"></div>
    <div class="background">
      <div class="tiles">
        <div class="tile tile-1"></div>
        <div class="tile tile-2"></div>
        <div class="tile tile-3"></div>
        <div class="tile tile-4"></div>

        <div class="tile tile-5"></div>
        <div class="tile tile-6"></div>
        <div class="tile tile-7"></div>
        <div class="tile tile-8"></div>

        <div class="tile tile-9"></div>
        <div class="tile tile-10"></div>
      </div>

      <div class="line line-1"></div>
      <div class="line line-2"></div>
      <div class="line line-3"></div>
    </div>
  </div>
</div>

<label class="day-night">
  <input type="checkbox" checked />
  <div></div>
</label>

<!-- twitter -->
<a class="twitter" target="_top" href="https://twitter.com/aaroniker_me"><svg xmlns="http://www.w3.org/2000/svg" width="72" height="72" viewBox="0 0 72 72"><path d="M67.812 16.141a26.246 26.246 0 0 1-7.519 2.06 13.134 13.134 0 0 0 5.756-7.244 26.127 26.127 0 0 1-8.313 3.176A13.075 13.075 0 0 0 48.182 10c-7.229 0-13.092 5.861-13.092 13.093 0 1.026.118 2.021.338 2.981-10.885-.548-20.528-5.757-26.987-13.679a13.048 13.048 0 0 0-1.771 6.581c0 4.542 2.312 8.551 5.824 10.898a13.048 13.048 0 0 1-5.93-1.638c-.002.055-.002.11-.002.162 0 6.345 4.513 11.638 10.504 12.84a13.177 13.177 0 0 1-3.449.457c-.846 0-1.667-.078-2.465-.231 1.667 5.2 6.499 8.986 12.23 9.09a26.276 26.276 0 0 1-16.26 5.606A26.21 26.21 0 0 1 4 55.976a37.036 37.036 0 0 0 20.067 5.882c24.083 0 37.251-19.949 37.251-37.249 0-.566-.014-1.134-.039-1.694a26.597 26.597 0 0 0 6.533-6.774z"></path></svg></a>
```
```scss
body {
  --background-color: #18181B;
  --text-color: #A1A1AA;

  --card-background-color: rgba(255, 255, 255, .015);
  --card-border-color: rgba(255, 255, 255, 0.1);
  --card-box-shadow-1: rgba(0, 0, 0, 0.05);
  --card-box-shadow-1-y: 3px;
  --card-box-shadow-1-blur: 6px;
  --card-box-shadow-2: rgba(0, 0, 0, 0.1);
  --card-box-shadow-2-y: 8px;
  --card-box-shadow-2-blur: 15px;
  --card-label-color: #FFFFFF;
  --card-icon-color: #D4D4D8;
  --card-icon-background-color: rgba(255, 255, 255, 0.08);
  --card-icon-border-color: rgba(255, 255, 255, 0.12);
  --card-shine-opacity: .1;
  --card-shine-gradient: conic-gradient(from 205deg at 50% 50%, rgba(16, 185, 129, 0) 0deg, #10B981 25deg, rgba(52, 211, 153, 0.18) 295deg, rgba(16, 185, 129, 0) 360deg);
  --card-line-color: #2A2B2C;
  --card-tile-color: rgba(16, 185, 129, 0.05);

  --card-hover-border-color: rgba(255, 255, 255, 0.2);
  --card-hover-box-shadow-1: rgba(0, 0, 0, 0.04);
  --card-hover-box-shadow-1-y: 5px;
  --card-hover-box-shadow-1-blur: 10px;
  --card-hover-box-shadow-2: rgba(0, 0, 0, 0.3);
  --card-hover-box-shadow-2-y: 15px;
  --card-hover-box-shadow-2-blur: 25px;
  --card-hover-icon-color: #34D399;
  --card-hover-icon-background-color: rgba(52, 211, 153, 0.1);
  --card-hover-icon-border-color: rgba(52, 211, 153, 0.2);

  --blur-opacity: .01;

  &.light {
    --background-color: #FAFAFA;
    --text-color: #52525B;

    --card-background-color: transparent;
    --card-border-color: rgba(24, 24, 27, 0.08);
    --card-box-shadow-1: rgba(24, 24, 27, 0.02);
    --card-box-shadow-1-y: 3px;
    --card-box-shadow-1-blur: 6px;
    --card-box-shadow-2: rgba(24, 24, 27, 0.04);
    --card-box-shadow-2-y: 2px;
    --card-box-shadow-2-blur: 7px;
    --card-label-color: #18181B;
    --card-icon-color: #18181B;
    --card-icon-background-color: rgba(24, 24, 27, 0.04);
    --card-icon-border-color: rgba(24, 24, 27, 0.1);
    --card-shine-opacity: .3;
    --card-shine-gradient: conic-gradient(from 225deg at 50% 50%, rgba(16, 185, 129, 0) 0deg, #10B981 25deg, #EDFAF6 285deg, #FFFFFF 345deg, rgba(16, 185, 129, 0) 360deg);
    --card-line-color: #E9E9E7;
    --card-tile-color: rgba(16, 185, 129, 0.08);

    --card-hover-border-color: rgba(24, 24, 27, 0.15);
    --card-hover-box-shadow-1: rgba(24, 24, 27, 0.05);
    --card-hover-box-shadow-1-y: 3px;
    --card-hover-box-shadow-1-blur: 6px;
    --card-hover-box-shadow-2: rgba(24, 24, 27, 0.1);
    --card-hover-box-shadow-2-y: 8px;
    --card-hover-box-shadow-2-blur: 15px;
    --card-hover-icon-color: #18181B;
    --card-hover-icon-background-color: rgba(24, 24, 27, 0.04);
    --card-hover-icon-border-color: rgba(24, 24, 27, 0.34);

    --blur-opacity: .1;
  }

  &.toggle .grid * {
    transition-duration: 0s !important;
  }
}

.grid {
  display: grid;
  grid-template-columns: repeat(2, 240px);
  grid-gap: 32px;
  position: relative;
  z-index: 1;
}

.card {
  background-color: var(--background-color);
  box-shadow: 0px var(--card-box-shadow-1-y) var(--card-box-shadow-1-blur) var(--card-box-shadow-1), 0px var(--card-box-shadow-2-y) var(--card-box-shadow-2-blur) var(--card-box-shadow-2), 0 0 0 1px var(--card-border-color);
  padding: 56px 16px 16px 16px;
  border-radius: 15px;
  cursor: pointer;
  position: relative;
  transition: box-shadow .25s;

  &::before {
    content: '';
    position: absolute;
    inset: 0;
    border-radius: 15px;
    background-color: var(--card-background-color);
  }

  .icon {
    z-index: 2;
    position: relative;
    display: table;
    padding: 8px;

    &::after {
      content: '';
      position: absolute;
      inset: 4.5px;
      border-radius: 50%;
      background-color: var(--card-icon-background-color);
      border: 1px solid var(--card-icon-border-color);
      backdrop-filter: blur(2px);
      transition: background-color .25s, border-color .25s;
    }

    svg {
      position: relative;
      z-index: 1;
      display: block;
      width: 24px;
      height: 24px;
      transform: translateZ(0);
      color: var(--card-icon-color);
      transition: color .25s;
    }
  }

  h4 {
    z-index: 2;
    position: relative;
    margin: 12px 0 4px 0;
    font-family: inherit;
    font-weight: 600;
    font-size: 14px;
    line-height: 2;
    color: var(--card-label-color);
  }

  p {
    z-index: 2;
    position: relative;
    margin: 0;
    font-size: 14px;
    line-height: 1.7;
    color: var(--text-color);
  }

  .shine {
    border-radius: inherit;
    position: absolute;
    inset: 0;
    z-index: 1;
    overflow: hidden;
    opacity: 0;
    transition: opacity .5s;

    &:before {
      content: '';
      width: 150%;
      padding-bottom: 150%;
      border-radius: 50%;
      position: absolute;
      left: 50%;
      bottom: 55%;
      filter: blur(35px);
      opacity: var(--card-shine-opacity);
      transform: translateX(-50%);
      background-image: var(--card-shine-gradient);
    }
  }

  .background {
    border-radius: inherit;
    position: absolute;
    inset: 0;
    overflow: hidden;
    -webkit-mask-image: radial-gradient(circle at 60% 5%, black 0%, black 15%, transparent 60%);
    mask-image: radial-gradient(circle at 60% 5%, black 0%, black 15%, transparent 60%);

    .tiles {
      opacity: 0;
      transition: opacity .25s;

      .tile {
        position: absolute;
        background-color: var(--card-tile-color);
        animation-duration: 8s;
        animation-iteration-count: infinite;
        opacity: 0;

        &.tile-4,
        &.tile-6,
        &.tile-10 {
          animation-delay: -2s;
        }

        &.tile-3,
        &.tile-5,
        &.tile-8 {
          animation-delay: -4s;
        }

        &.tile-2,
        &.tile-9 {
          animation-delay: -6s;
        }

        &.tile-1 {
          top: 0;
          left: 0;
          height: 10%;
          width: 22.5%;
        }

        &.tile-2 {
          top: 0;
          left: 22.5%;
          height: 10%;
          width: 27.5%;
        }

        &.tile-3 {
          top: 0;
          left: 50%;
          height: 10%;
          width: 27.5%;
        }

        &.tile-4 {
          top: 0;
          left: 77.5%;
          height: 10%;
          width: 22.5%;
        }

        &.tile-5 {
          top: 10%;
          left: 0;
          height: 22.5%;
          width: 22.5%;
        }

        &.tile-6 {
          top: 10%;
          left: 22.5%;
          height: 22.5%;
          width: 27.5%;
        }

        &.tile-7 {
          top: 10%;
          left: 50%;
          height: 22.5%;
          width: 27.5%;
        }

        &.tile-8 {
          top: 10%;
          left: 77.5%;
          height: 22.5%;
          width: 22.5%;
        }

        &.tile-9 {
          top: 32.5%;
          left: 50%;
          height: 22.5%;
          width: 27.5%;
        }

        &.tile-10 {
          top: 32.5%;
          left: 77.5%;
          height: 22.5%;
          width: 22.5%;
        }
      }
    }

    @keyframes tile {

      0%,
      12.5%,
      100% {
        opacity: 1;
      }

      25%,
      82.5% {
        opacity: 0;
      }
    }

    .line {
      position: absolute;
      inset: 0;
      opacity: 0;
      transition: opacity .35s;

      &:before,
      &:after {
        content: '';
        position: absolute;
        background-color: var(--card-line-color);
        transition: transform .35s;
      }

      &:before {
        left: 0;
        right: 0;
        height: 1px;
        transform-origin: 0 50%;
        transform: scaleX(0);
      }

      &:after {
        top: 0;
        bottom: 0;
        width: 1px;
        transform-origin: 50% 0;
        transform: scaleY(0);
      }

      &.line-1 {
        &:before {
          top: 10%;
        }

        &:after {
          left: 22.5%;
        }

        &:before,
        &:after {
          transition-delay: .3s;
        }
      }

      &.line-2 {
        &:before {
          top: 32.5%;
        }

        &:after {
          left: 50%;
        }

        &:before,
        &:after {
          transition-delay: .15s;
        }
      }

      &.line-3 {
        &:before {
          top: 55%;
        }

        &:after {
          right: 22.5%;
        }
      }
    }
  }

  &:hover {
    box-shadow: 0px 3px 6px var(--card-hover-box-shadow-1), 0px var(--card-hover-box-shadow-2-y) var(--card-hover-box-shadow-2-blur) var(--card-hover-box-shadow-2), 0 0 0 1px var(--card-hover-border-color);

    .icon {
      &::after {
        background-color: var(--card-hover-icon-background-color);
        border-color: var(--card-hover-icon-border-color);
      }

      svg {
        color: var(--card-hover-icon-color);
      }
    }

    .shine {
      opacity: 1;
      transition-duration: .5s;
      transition-delay: 0s;
    }

    .background {

      .tiles {
        opacity: 1;
        transition-delay: .25s;

        .tile {
          animation-name: tile;
        }
      }

      .line {
        opacity: 1;
        transition-duration: .15s;

        &:before {
          transform: scaleX(1);
        }

        &:after {
          transform: scaleY(1);
        }

        &.line-1 {

          &:before,
          &:after {
            transition-delay: .0s;
          }
        }

        &.line-2 {

          &:before,
          &:after {
            transition-delay: .15s;
          }
        }

        &.line-3 {

          &:before,
          &:after {
            transition-delay: .3s;
          }
        }
      }
    }
  }
}

.day-night {
  cursor: pointer;
  position: absolute;
  right: 20px;
  top: 20px;
  opacity: .3;

  input {
    display: none;

    &+div {
      border-radius: 50%;
      width: 20px;
      height: 20px;
      position: relative;
      box-shadow: inset 8px -8px 0 0 var(--text-color);
      transform: scale(1) rotate(-2deg);
      transition: box-shadow .5s ease 0s, transform .4s ease .1s;

      &:before {
        content: '';
        width: inherit;
        height: inherit;
        border-radius: inherit;
        position: absolute;
        left: 0;
        top: 0;
        transition: background-color .3s ease;
      }

      &:after {
        content: '';
        width: 6px;
        height: 6px;
        border-radius: 50%;
        margin: -3px 0 0 -3px;
        position: absolute;
        top: 50%;
        left: 50%;
        box-shadow: 0 -23px 0 var(--text-color), 0 23px 0 var(--text-color), 23px 0 0 var(--text-color), -23px 0 0 var(--text-color), 15px 15px 0 var(--text-color), -15px 15px 0 var(--text-color), 15px -15px 0 var(--text-color), -15px -15px 0 var(--text-color);
        transform: scale(0);
        transition: all .3s ease;
      }
    }

    &:checked+div {
      box-shadow: inset 20px -20px 0 0 var(--text-color);
      transform: scale(.5) rotate(0deg);
      transition: transform .3s ease .1s, box-shadow .2s ease 0s;

      &:before {
        background: var(--text-color);
        transition: background-color .3s ease .1s;
      }

      &:after {
        transform: scale(1);
        transition: transform .5s ease .15s;
      }
    }
  }
}

html {
  box-sizing: border-box;
  -webkit-font-smoothing: antialiased;
}

* {
  box-sizing: inherit;
  &:before,
  &:after {
    box-sizing: inherit;
  }
}

// Center
body {
  min-height: 100vh;
  display: flex;
  font-family: 'Inter', Arial;
  justify-content: center;
  align-items: center;
  background-color: var(--background-color);
  overflow: hidden;

  &:before {
    content: '';
    position: absolute;
    inset: 0 -60% 65% -60%;
    background-image: radial-gradient(ellipse at top, #10B981 0%, var(--background-color) 50%);
    opacity: var(--blur-opacity);
  }

  .twitter {
    position: fixed;
    display: block;
    right: 12px;
    bottom: 12px;
    svg {
      width: 32px;
      height: 32px;
      fill: #fff;
    }
  }
}
```
===


32. Animated info card

Pure CSS Responsive animated info card

Loading interactive CodePen...

Open on CodePen
When a user hovers over the animated info card by Chris Smith, its green overlay slides to the left, revealing a text section with detailed information about trees. This card relies on three animations — slide, slide-left, and slide-up to function.

```html
<div class="wrap animate pop">
	<div class="overlay">
		<div class="overlay-content animate slide-left delay-2">
			<h1 class="animate slide-left pop delay-4">Trees</h1>
			<p class="animate slide-left pop delay-5" style="color: white; margin-bottom: 2.5rem;">Kingdom: <em>Plantae</em></p>
		</div>
		<div class="image-content animate slide delay-5"></div>
		<div class="dots animate">
			<div class="dot animate slide-up delay-6"></div>
			<div class="dot animate slide-up delay-7"></div>
			<div class="dot animate slide-up delay-8"></div>
		</div>
	</div>
	<div class="text">
		<p><img class="inset" src="https://assets.codepen.io/4787486/oak_1.jpg" alt="" />Trees are woody perennial plants that are a member of the kingdom <em>Plantae</em>. All species of trees are grouped by their genus, family, and order. This helps make identifying and studying trees easier.</p>
		<p>Apart from providing oxygen for the planet and beauty when they bloom or turn color, trees are very useful. Certain species of hardwood and softwood trees are excellent for timber, making furniture, and paper.</p>
		<p>When managed properly, trees are a good source of renewable energy and construction material.</p>
		<img class="tree" src="https://assets.codepen.io/4787486/tree+clipart.jpeg" alt="" />
	</div>
</div>
```
```css
* {
	padding: 0;
	margin: 0;
	box-sizing: border-box;
}
body {
	min-height: 100vh;
	display: grid;
	place-items: center;
	margin: 0;
	padding: 0;
	background: linear-gradient(
		to right,
		#008000,
		#00e600,
		#b3ffb3,
		#00e600,
		#008000
	);
	font-family: "Martel Sans", sans-serif;
}

h1 {
	font-size: 5.25vmin;
	text-align: center;
	color: white;
}
p {
	font-size: max(10pt, 2.5vmin);
	line-height: 1.4;
	color: #0e390e;
	margin-bottom: 1.5rem;
}

.wrap {
	display: flex;
	flex-wrap: nowrap;
	justify-content: space-between;
	width: 85vmin;
	height: 65vmin;
	margin: 2rem auto;
	border: 8px solid;
	border-image: linear-gradient(
			-50deg,
			green,
			#00b300,
			forestgreen,
			green,
			lightgreen,
			#00e600,
			green
		)
		1;
	transition: 0.3s ease-in-out;
	position: relative;
	overflow: hidden;
}
.overlay {
	position: relative;
	display: flex;
	width: 100%;
	height: 100%;
	padding: 1rem 0.75rem;
	background: #186218;
	transition: 0.4s ease-in-out;
	z-index: 1;
}
.overlay-content {
	display: flex;
	flex-direction: column;
	justify-content: space-between;
	width: 15vmin;
	height: 100%;
	padding: 0.5rem 0 0 0.5rem;
	border: 3px solid;
	border-image: linear-gradient(
			to bottom,
			#aea724 5%,
			forestgreen 35% 65%,
			#aea724 95%
		)
		0 0 0 100%;
	transition: 0.3s ease-in-out 0.2s;
	z-index: 1;
}
.image-content {
	position: absolute;
	top: 0;
	right: 0;
	width: 60vmin;
	height: 100%;
	background-image: url("https://assets.codepen.io/4787486/trees.png");
	background-size: cover;
	transition: 0.3s ease-in-out;
	/* border: 1px solid green; */
}

.inset {
	max-width: 50%;
	margin: 0.25em 1em 1em 0;
	border-radius: 0.25em;
	float: left;
}

.dots {
	position: absolute;
	bottom: 1rem;
	right: 2rem;
	display: flex;
	flex-direction: row;
	justify-content: space-around;
	align-items: center;
	width: 70px;
	height: 4vmin;
	transition: 0.3s ease-in-out 0.3s;
}
.dot {
	width: 1rem;
	height: 1rem;
	background: yellow;
	border: 1px solid indigo;
	border-radius: 50%;
	transition: 0.3s ease-in-out 0.3s;
}

.text {
	display: grid;
	position: absolute;
	top: 0;
	right: 0;
	width: 60vmin;
	height: 100%;
	padding: 3vmin 4vmin;
	background: #fff;
	box-shadow: inset 1px 1px 15px 0 rgba(0 0 0 / 0.4);
	overflow-y: scroll;
}
.tree {
	place-self: center;
	width: calc(50px + 2vw);
}

.wrap:hover .overlay {
	transform: translateX(-60vmin);
}
.wrap:hover .image-content {
	width: 30vmin;
}
.wrap:hover .overlay-content {
	border: none;
	transition-delay: 0.2s;
	transform: translateX(60vmin);
}
.wrap:hover .dots {
	transform: translateX(1rem);
}
.wrap:hover .dots .dot {
	background: white;
}

/* Animations and timing delays */
.animate {
	animation-duration: 0.7s;
	animation-timing-function: cubic-bezier(0.26, 0.53, 0.74, 1.48);
	animation-fill-mode: backwards;
}
/* Pop In */
.pop {
	animation-name: pop;
}
@keyframes pop {
	0% {
		opacity: 0;
		transform: scale(0.5, 0.5);
	}
	100% {
		opacity: 1;
		transform: scale(1, 1);
	}
}

/* Slide In */
.slide {
	animation-name: slide;
}
@keyframes slide {
	0% {
		opacity: 0;
		transform: translate(4em, 0);
	}
	100% {
		opacity: 1;
		transform: translate(0, 0);
	}
}

/* Slide Left */
.slide-left {
	animation-name: slide-left;
}
@keyframes slide-left {
	0% {
		opacity: 0;
		transform: translate(-40px, 0);
	}
	100% {
		opacity: 1;
		transform: translate(0, 0);
	}
}

.slide-up {
	animation-name: slide-up;
}
@keyframes slide-up {
	0% {
		opacity: 0;
		transform: translateY(3em);
	}
	100% {
		opacity: 1;
		transform: translateY(0);
	}
}

.delay-1 {
	animation-delay: 0.3s;
}
.delay-2 {
	animation-delay: 0.6s;
}
.delay-3 {
	animation-delay: 0.9s;
}
.delay-4 {
	animation-delay: 1.2s;
}
.delay-5 {
	animation-delay: 1.5s;
}
.delay-6 {
	animation-delay: 1.8s;
}
.delay-7 {
	animation-delay: 2.1s;
}
.delay-8 {
	animation-delay: 2.4s;
}

```

===


33. Flipping split card

CSS | Flipping Card Split

The flipping split card effect by Tobias Bogliolo creates a flippable card effect using CSS transforms and perspective, where a wrapper contains a flip container with front and back sides. When a user hovers over the wrapper, the .flip-left and .flip-right elements rotate 180 degrees along the Y-axis, creating a smooth flipping animation.

The backface-visibility: hidden (docs) property ensures that only one side of the card is visible during the rotation, while the perspective and transform-style: preserve-3d (docs) properties give the flip a three-dimensional feel.

```html
<!-- CSS - Cards - Flip 360 -->

<div class="wrapper">
  <div class="flip-container">
    <div class="flip flip-left">
      <div class="front front-left">
          <div class="circle"></div>
      </div>
      <div class="back back-left">
        <img src="https://drive.google.com/uc?id=1tpr7twQMFTss3kriJWfmwQhdZPM-F_8w" />
      </div>
    </div>
  </div><div class="flip-container">
    <div class="flip flip-right">
      <div class="front front-right">
          <div class="circle"></div>
      </div>
      <div class="back back-right">
        <img src="https://drive.google.com/uc?id=1tpr7twQMFTss3kriJWfmwQhdZPM-F_8w" />
      </div>
    </div>
  </div>
</div>
```
```css
body {
  background: #1976D2;
}

.wrapper {
  display: inline-block;
  background: #01579B;
  box-shadow: 0 3px 6px rgba(0,0,0,0.16), 0 3px 6px rgba(0,0,0,0.23);
  position: absolute;
  top: 50%;
  left: 50%;
  margin-top: -120px;
  margin-left: -180px;
}

/* entire container, keeps perspective */
.flip-container {
  display: inline-block;
  perspective: 1000px;
  vertical-align: top; /* Prevent unwanted margin. */
}

/* flip the pane when hovered */

.wrapper:hover .flip-left {
  transform: rotateY(-180deg);
}

.wrapper:hover .flip-right {
  transform: rotateY(180deg);
}

.flip-container,
.front,
.back {
  width: 180px;
  height: 240px;
}

/* flip speed goes here */
.flip {
  transition: 0.6s;
  transform-style: preserve-3d;
  position: relative;
}

/* hide back of pane during swap */
.front,
.back {
  backface-visibility: hidden;
  position: absolute;
  top: 0;
  left: 0;
}

/* front pane, placed above back */
.front {
  z-index: 2;
  background: #2196F3;
  transform: rotateY(0deg);
  overflow: hidden;
  /*position: relative;*/
}

.front-left .circle {
  position: absolute;
  right: -50px;
  top: 50%;
  margin-top: -50px;
}

.front-right .circle {
  position: absolute;
  left: -50px;
  top: 50%;
  margin-top: -50px;
}

.circle {
  position: absolute;
  width: 100px;
  height: 100px;
  border-radius: 100%;
  background: #ffffff;  box-shadow: 0 19px 38px rgba(0,0,0,0.20), 0 15px 12px rgba(0,0,0,0.12);

}

/* back, initially hidden pane */
.back {
  background: rgb(220,220,220);
  transform: rotateY(180deg);
  overflow: hidden;
}

img {
  width: 360px;
  height: 240px;
}

.back-left img {
  position: absolute;
  left: 0;
}

.back-right img {
  position: absolute;
  right: 0;
}

```
===


34. House of CSS cards

House of CSS cards


The house of CSS cards animation by Amit Sheen creates a 3D card tower that rotates continuously. The tower is built using multiple nested .card elements; each positioned using precise rotateY, translateX, and translateY transformations to create the circular structure.

A towerRotate animation spins the entire structure at a slow, consistent pace. As the tower rotates, visitors can change the viewing angle by moving their mouse over different parts of the screen, which makes the tower look like it's shifting perspective.
```html
<input type="checkbox" id="wf" class="wireframe_input">
<label for="wf" class="wireframe_label">Wireframe</label>

<div class="hoverPad"></div>
<div class="hoverPad"></div>
<div class="hoverPad"></div>
<div class="hoverPad"></div>
<div class="hoverPad"></div>
<div class="hoverPad"></div>
<div class="hoverPad"></div>
<div class="hoverPad"></div>
<div class="hoverPad"></div>
<div class="hoverPad"></div>

<div class="towerOuter">
  <div class="tower">
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>

    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>

    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>
    <div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div><div class="card"></div>

    <div class="shadow"></div>
  </div>
</div>
```
```scss
@import url('https://fonts.googleapis.com/css2?family=Anton&display=swap');

*, *::before, *::after {
    padding: 0;
    margin: 0 auto;
    box-sizing: border-box;
}

:root {
    --perspectivOriginY: 25%;
}

body {
    font-family: 'Anton', sans-serif;
    background-color: #151515;
    color: #aaa;
    min-height: 100vh;
    display: flex;
    justify-content: center;
    align-items: center;
    overflow: hidden;
}

.wireframe {

    &_input {
        position: fixed;
        top: -100%;
        left: -100%;
        visibility: hidden;

        &:checked {
            
            & + .wireframe_label {
                color: chartreuse;
                &::before {
                    background-color: chartreuse;
                }
            }

            & ~ .towerOuter > .tower > .card, & ~ .towerOuter > .tower > .card::after {
                background-image: unset;
                box-shadow: 0 0 1px 1px #fff inset;
            }
        }
    }

    &_label {
        position: fixed;
        top: 0%;
        left: 0%;
        font-size: 20px;
        padding: 0.5em;
        z-index: 3;
        transition: color 0.25s;

        &::before {
            content: '';
            display: inline-block;
            width: 0.75em;
            height: 0.75em;
            margin-right: 0.25em;
            background-color: maroon;
            border-radius: 0.25em;
            border: 2px solid #aaa;
            transition: background-color 0.25s;
        }
    }
}

.hoverPad {
    position: absolute;
    z-index: 2;
    width: 100%;
    height: 10%;

    @for $y from 0 to 10 {
        &:nth-child(#{$y + 3}) {
            top: #{$y * 10%};

            &:hover ~ .towerOuter {
                --perspectivOriginY: #{$y * 5% + 15%};
            }
        }
    }
}

// card vars:
$towerRadius: 210;
$cardWidth: 50;
$cardHeight: 70;
$ringHeight: 33;

.towerOuter {
    position: absolute;
    top: 0; left: 0; right: 0; bottom: 0;
    perspective: 700px;
    perspective-origin: center var(--perspectivOriginY);
    transition: perspective-origin 0.25s;
}

.tower {
    position: absolute;
    top: 50%; left: 50%;;
    width: $cardWidth + px;
    transform-style: preserve-3d;
    animation: towerRotate 60s infinite linear;

    @keyframes towerRotate {
        from { transform: translateX(-50%) rotateY(0deg); }
        to { transform: translateX(-50%) rotateY(360deg); }
    }
}

.card {
    position: absolute;
    width: $cardWidth + px;
    height: $cardHeight + px;
    background-image: url('https://assets.codepen.io/1948355/cardsheet01.jpg');
    background-size: #{$cardWidth * 13}px #{$cardHeight * 4}px;
    border-radius: 8px;
    transform-style: preserve-3d;
    box-shadow: 0 0 10px #0007 inset;
    backface-visibility: hidden;

    @for $r from 0 through 2 {
        @for $i from 1 through 24 {
            &:nth-child(#{$i + $r*72}) {
                background-position: #{random(13) * $cardWidth}px #{random(4) * $cardHeight}px;
                // transform: rotateY(#{($i - 1 + $r * 0.5) * 360 / 24}deg) translateX(#{$towerRadius}px) translateY(#{($r - 1.5) * ($ringHeight * 2)}px) rotateX(#{((random(2) - 1) * 180) - 20}deg);
                transform: rotateY(#{($i - 1 + $r * 0.5) * 360 / 24}deg) translateX(#{$towerRadius}px) translateY(#{($r - 1.5) * ($ringHeight * 2)}px) rotateX(-20deg);
            }
            &:nth-child(#{$i + 24 + $r*72}) {
                background-position: #{random(13) * $cardWidth}px #{random(4) * $cardHeight}px;
                // transform: rotateY(#{($i - 24.5 + $r * 0.5) * 360 / 24}deg) translateX(#{$towerRadius}px) translateY(#{($r - 1.5) * ($ringHeight * 2)}px) rotateX(#{((random(2) - 1) * 180) + 20}deg);
                transform: rotateY(#{($i - 24.5 + $r * 0.5) * 360 / 24}deg) translateX(#{$towerRadius}px) translateY(#{($r - 1.5) * ($ringHeight * 2)}px) rotateX(200deg);
            }
            &:nth-child(#{$i + 48 + $r*72}) {
                background-position: #{random(13) * $cardWidth}px #{random(4) * $cardHeight}px;
                // transform: rotateY(#{($i - 48.75 + $r * 0.5) * 360 / 24}deg) translateX(#{$towerRadius}px) translateY(#{($r - 2) * ($ringHeight * 2)}px) rotateX(#{((random(2) - 1) * 180) - 90}deg);
                transform: rotateY(#{($i - 48.75 + $r * 0.5) * 360 / 24}deg) translateX(#{$towerRadius}px) translateY(#{($r - 2) * ($ringHeight * 2)}px) rotateX(-90deg);
            }
        }
    }

    &::after {
        content: '';
        position: absolute;
        top: 0; left: 0;
        width: $cardWidth + px;
        height: $cardHeight + px;
        transform: rotateY(180deg);
        backface-visibility: hidden;
        background-image: url('https://assets.codepen.io/1948355/cardback01.jpg');
        background-size: #{$cardWidth}px #{$cardHeight}px;
        border: 1px solid #fff7;
        border-radius: 8px;
    }
}

.shadow {
    position: absolute;
    top: 50%; left: 50%;
    transform: translate(-50%, -50%) rotateX(-90deg) translateZ(#{($ringHeight * 3) + 5}px);
    width: #{$towerRadius * 3}px;
    height: #{$towerRadius * 3}px;
    background-image: radial-gradient(#0000, #000 #{$towerRadius}px, #0000 #{$towerRadius * 1.5}px);
}
```

===


Other creative animations

===


35. Watch tower

Watch Tower Pure CSS Animation

The watch tower animation by Travis Doughty features flying birds and moving clouds and is a great reminder that there are almost no limits to what one can create with CSS and HTML alone. This animation works with clip path, CSS transforms, and several keyframe animations.
```html
.container
	.birds.front
		-	for(var x = 1; x <= 12; x++)
			div(class="bird b" + x)
				.body
				.wing1
					.wing2
						.wing3
	.birds.back
		-	for(var x = 1; x <= 12; x++)
			div(class="bird b" + x)
				.body
				.wing1
					.wing2
						.wing3
	.cloud.big
		-	for(var x = 0; x <= 8; x++)
			div(class="circle c" + x)
	.cloud.small
		-	for(var x = 0; x <= 8; x++)
			div(class="circle c" + x)
	.mountain
		.backdrop
		-	for(var x = 0; x <= 4; x++)
			div(class="zig zag" + x)
			.top
			.mid
			.bot
			.base
	.range
		-	for(var x = 1; x <= 7; x++)
			div(class="r" + x)
	-	for(var x = 1; x <= 8; x++)
		div(class="tree treeBack tree" + x)
			.top
			.mid
			.bot
			.base
	.tower
		.shadow
		.flagPole
		.roof1
		.roof2
		.wall
			- for(var x = 1; x <= 5; x++)
				div(class="w" + x)
		.legs
			.left
			.right
			.support1
				.criss
				.cross
				.flat
			.support2
				.criss
				.cross
				.flat
		.railing
			.top
			.bot1
			.bot2
			- for(var x = 1; x <= 9; x++)
				div(class="r" + x)
	-	for(var x = 1; x <= 5; x++)
		div(class="tree treeMid tree" + x)
			.top
			.mid
			.bot
			.base
	-	for(var x = 1; x <= 4; x++)
		div(class="tree treeFront tree" + x)
			.top
			.mid
			.bot
			.base
	
```
```scss
$tree1: #2D1427;
$tree2: #5A0831;
$tree3: #CD4D45;
$range: #F46435;
$mountain1: #F59452;
$mountain2: #F47A45;
$wood: #370D09;
$pole: #791819;
$flag: #C63737;
$tower1: #76122C;
$tower2: #C93D3D;
$tower3: #821021;
$tower4: #F4633A;
$tower5: #4B1205;
$tower6: #C13C45;

.container {
	position: relative;
	width: 160px;
	height: 335px;
	background: #FAE0C8;
	border-radius: 100px;
	overflow: hidden;
}

.mountain {
	position: absolute;
	top: 0;
	opacity: 1;
	.backdrop {
		position: absolute;
		top: 80px;
		left: -180px;
		width: 0;
		height: 0;
		border-left: 260px solid transparent;
		border-right: 260px solid transparent;
		border-bottom: 200px solid $mountain1;
	}
	.zig {
		position: absolute;
		width: 0;
		height: 0;
		transform: rotate(217deg);
		&.zag1 {
			top: 83px;
			left: 70px;
			border-left: 10px solid transparent;
			border-right: 10px solid transparent;
			border-bottom: 30px solid $mountain2;	
		}
		&.zag2 {
			top: 94px;
			left: 70px;
			border-left: 20px solid transparent;
			border-right: 20px solid transparent;
			border-bottom: 60px solid $mountain2;	
		}
		&.zag3 {
			top: 115px;
			left: 84px;
			border-left: 30px solid transparent;
			border-right: 30px solid transparent;
			border-bottom: 80px solid $mountain2;	
		}
		&.zag4 {
			top: 137px;
			left: 100px;
			border-left: 40px solid transparent;
			border-right: 40px solid transparent;
			border-bottom: 100px solid $mountain2;	
		}
	}
}

.tree {
	opacity: 1;
	position: absolute;
	& > div {
		position: absolute;
	}
	&.treeFront {
		> div {
			border-bottom-color: $tree1;
		}
		&.tree1 { top: 200px; left: 0px; }
		&.tree2 {	top: 220px; left: 52px; }
		&.tree3 { top: 238px; left: 94px; }
		&.tree4 {	top: 224px;	left: 136px; }
	}
	&.treeMid {
		> div {
			border-bottom-color: $tree2;
		}
		&.tree1 { top: 225px; left: 27px; }
		&.tree2 { top: 232px; left: 67px; }
		&.tree3 { top: 225px; left: 86px; }
		&.tree4 { top: 223px; left: 106px; }
		&.tree5 { top: 215px; left: 127px; }
	}
	&.treeBack {
		> div {
			border-bottom-color: $tree3;
		}
		&.tree1 { top: 202px; left: -12px; }
		&.tree2 { top: 204px; left: 17px; }
		&.tree3 { top: 212px; left: 40px; }
		&.tree4 { top: 210px; left: 60px; }
		&.tree5 { top: 208px; left: 80px; }
		&.tree6 { top: 210px; left: 98px; }
		&.tree7 { top: 204px; left: 115px; }
		&.tree8 { top: 202px; left: 130px; }
	}
	.top {
		border-left: 17px solid transparent;
		border-right: 17px solid transparent;
		border-bottom: 45px solid #000;
	}
	.mid {
		top: 16px;
		left: -7px;
		border-left: 24px solid transparent;
		border-right: 24px solid transparent;
		border-bottom: 58px solid #000;
	}
	.bot {
		top: 30px;
		left: -12px;
		border-left: 29px solid transparent;
		border-right: 29px solid transparent;
		border-bottom: 68px solid #000;
	}
	.base {
		top: 44px;
		left: -16px;
		border-left: 33px solid transparent;
		border-right: 33px solid transparent;
		border-bottom: 75px solid blue;
	}
}

.range {
	position: absolute;
	top: 0;
	opacity: 1;
	> div {
		position: absolute;
		background: $range;
		width: 60px;
		height: 50px;
	}
	.r1 {
		top: 200px;
		left: -22px;
		width: 60px;
		height: 50px;
		transform: rotate(34deg);
	}
	.r2 {
		top: 198px;
		left: -20px;
		transform: rotate(-8deg);
	}
	.r3 {
		top: 205px;
		left: 24px;
		transform: rotate(25deg);
	}
	.r4 {
		top: 205px;
		left: 50px;
		transform: rotate(-28deg);
	}
	.r5 {
		top: 200px;
		left: 88px;
		transform: rotate(14deg);
	}
	.r6 {
		top: 200px;
		left: 100px;
		transform: rotate(-38deg);
	}
	.r7 {
		top: 199px;
		left: 122px;
		transform: rotate(30deg);
	}
}

.tower {
	position: absolute;
	width: 74px;
	margin-top: 108px;
	margin-left: calc(50% - 37px);
	opacity: 1;
	.shadow {
		position: absolute;
		z-index: 9999;
		top: 12px;
		width: 100%;
		height: 42px;
		background: #000;
		clip-path: polygon(50% 0, 100% 40%, 100% 45%, 87% 45%, 87% 90%, 100% 90%, 100% 100%, 60% 100%, 60% 31%, 50% 0);
		opacity: .4;
	}
	.flagPole {
		width: 2px;
		height: 12px;
		background: $pole;
		margin-left: 36px;
		&:after {
			content: '';
			width: 12px;
			height: 6px;
			background: $flag;
			position: absolute;
			display: block;
		}
	}
	.roof1 {
		border-left: 34px solid transparent;
		border-right: 34px solid transparent;
		border-bottom: 15px solid $tower1;
	}
	.roof2 {
		width: 100%;
		height: 3px;
		background: $tower2;
	}
	.wall {
		position: relative;
		width: 76%;
		height: 22px;
		background: $tower3;
		margin-left: 12%;
		padding-top: 4px;
		.w1, .w2, .w3, .w4, .w5 {
			position: absolute;
			width: 8px;
			height: 14px;
			background: $tower4;
		}
		.w1 {
			left: 4px;
		}
		.w2 {
			left: 14px;
		}
		.w3 {
			left: 24px;
		}
		.w4 {
			left: 34px;
		}
		.w5 {
			left: 44px;
		}
	}
	.legs {
		position: relative;
		.left,
		.right {
			position: absolute;
			width: 4px;
			height: 150px;
			background: $wood;
		}
		.left {
			transform: rotate(3deg);
			left: 12px;
		}
		.right {
			transform: rotate(-3deg);
			right: 12px;
		}
		.support1,
		.support2 {
			position: absolute;
			.criss,
			.cross {
				position: absolute;
				left: 35px;
				width: 4px;
				height: 64px;
				background: $wood;
			}
			.criss {
				transform: rotate(45deg);
			}
			.cross {
				transform: rotate(-45deg);
			}
			.flat {
				position: absolute;
				width: 46px;
				height: 4px;
				background: $wood;
				bottom: -55px;
				left: 14px;
			}
		}
		.support1 {
			top: -14px;
		}
		.support2 {
			top: 28px;
		}
	}
	.railing {
		position: relative;
		top: -16px;
		.r1, .r2, .r3, .r4, .r5, .r6, .r7, .r8, .r9 {
			position: absolute;
			width: 2px;
			height: 10px;
			background: $wood;
		}
		.r1 { left: 5px; }
		.r2 {	left: 12px;	}
		.r3 {	left: 20px;	}
		.r4 {	left: 28px;	}
		.r5 {	left: 36px;	}
		.r6 {	left: 44px;	}
		.r7 {	left: 52px;	}
		.r8 {	left: 60px;	}
		.r9 {	right: 5px;	}
		.top,
		.bot1,
		.bot2 {
			position: absolute;
		}
		.top {
			width: 100%;
			height: 2px;
			background: $tower5;
		}
		.bot1 {
			width: 100%;
			height: 4px;
			top: 10px;
			background: $tower6;
		}
		.bot2 {
			width: 80%;
			height: 2px;
			top: 14px;
			left: 8px;
			background: $tower5;
			opacity: 1;
		}
	}
}

.cloud {
	position: absolute;
	width: 162px;
	height: 55px;
	overflow: hidden;
	&.big {
		top: 10px;
		transform: scale(.8);
		animation: bigCloud 4s linear;
		animation-iteration-count: infinite;
		animation-direction: forwards;
	}
	&.small {
		top: 70px;
		transform: scale(.4);
		animation: smallCloud 4s linear;
		animation-iteration-count: infinite;
		animation-direction: forwards;
		animation-delay: 3s;
	}
	.circle {
		position: absolute;
		border-radius: 50%;
		background: #FFF;
	}
	.c1 {
		width: 32px;
		height: 32px;
		bottom: -15px;
	}
	.c2 {
		width: 35px;
		height: 35px;
		left: 20px;
		bottom: 0;
	}
	.c3 {
		width: 25px;
		height: 25px;
		left: 48px;
		bottom: 15px;
	}
	.c4 {
		width: 35px;
		height: 35px;
		left: 65px;
		bottom: 20px;
	}
	.c5 {
		width: 25px;
		height: 25px;
		left: 94px;
		bottom: 16px;
	}
	.c6 {
		width: 30px;
		height: 30px;
		left: 110px;
		bottom: -5px;
	}
	.c7 {
		width: 30px;
		height: 30px;
		left: 132px;
		bottom: -15px;
	}
	.c8 {
		width: 90px;
		height: 90px;
		left: 30px;
		bottom: -55px;
		background: #FFF;
	}
}

@keyframes bigCloud {
	0% {
		transform: translateX(-200px) scale(.8);
	}
	100% {
		transform: translateX(200px) scale(.8);
	}
}

@keyframes smallCloud {
	0% {
		transform: translateX(-200px) scale(.4);
	}
	100% {
		transform: translateX(200px) scale(.4);
	}
}

.birds {
	position: absolute;
	z-index: 9999;
	width: 100px;
	height: 100px;
	&.front {
		animation: flyFront 4s linear;
		animation-direction: forwards;
		animation-iteration-count: infinite;
		animation-delay: .5s;
		top: 200px;
		left: 200px;
	}
	&.back {
		animation: flyBack 4s linear;
		animation-direction: forwards;
		animation-iteration-count: infinite;
		animation-delay: .5s;
		// animation-delay: 3s;
		top: 50px;
		left: -425px;
	}
	.bird {
		position: absolute;
		transform: scale(0.15);
	}
	@for $i from 1 through 12 {
		$delay: random(2) - .5 + s;
		.b#{$i} {
			.wing1 {
				animation: flap .5s ease-in-out;
				animation-iteration-count: infinite;
				//animation-timing-function: ease;
				animation-direction: alternate;
				transform-origin: 0 0;
				//animation-delay: $delay;
				animation-duration: $delay;
			}
		}
	}
	.b1 { top: -30px;	}
	.b2 { top: -20px; left: -15px; }
	.b3 { left: 10px;	}
	.b4 {	top: 15px; left: 20px; }
	.b5 { top: 30px; left: -5px;	}
	.b6 { top: 45px; left: 5px;	}
	.b7 { top: -5px; left: -35px;	}
	.b8 { top: 10px; left: -25px; }
	.b9 { top: 25px; left: -50px; }
	.b10 { top: 40px; left: -40px; }
	.b11 { top: -10px; left: -75px;	}
	.b12 { top: 5px; left: -65px;	}
}
.body {
	clip-path: polygon(0 100%, 20% 20%, 40% 0, 100% 100%, 20% 80%);
	background: #000;
	width: 150px;
	height: 40px;
}

.wing1 {
	position: relative;
	left: 40px;
	top: -20px;
	width: 40px;
	height: 50px;
	background: #000;
	transform: skew(10deg);
	.wing2 {
		position: absolute;
		bottom: -25px;
		left: 13px;
		
		transform: rotate(-5deg);
	}
	.wing3 {
		width: 40px;
		height: 30px;
		background: #000;
		transform: skew(40deg);
	}
}

@keyframes flap {
	0% {
		transform: skew(10deg) rotateX(50deg);
	}
	100% {
		transform: skew(15deg) rotateX(120deg);
	}
}

@keyframes flyFront {
	0% {
		transform: translate3d(0, 0, 0) rotate(15deg);
	}
	100% {
		transform: translate3d(-600px, -150px, 0) rotate(15deg);
	}
}

@keyframes flyBack {
	0% {
		transform: translate3d(0, 0, 0) scale(.6) scaleX(-1) rotate(-15deg);
	}
	100% {
		transform: translate3d(600px, -50px, 0) scale(.6) scaleX(-1) rotate(15deg);
	}
}

// floof
html, body {
	overflow: hidden;
}
body {
	background: #E6FFFF;
	width: 100vw;
	height: 100vh;
	display: flex;
	align-items: center;
  justify-content: center;
}

```
===


36. Funny candle

Funny Candle Pure CSS Animation


The funny candle animation by Kevin David could easily pass for a scene from a cartoon. The candles are brought to life through multiple keyframe animations that control their body expansion, eye blinking, mouth movements, and overall positioning, while additional elements like smoke, light waves, and a flickering fire add character to the scene.

Each element—from the candles' eyes and mouths to the smoke and waves—has its own unique animation sequence.
```html
<!DOCTYPE html>
<html lang="en" >
<head>
  <meta charset="UTF-8">
  <title>Candle Fun (Pure CSS Animation)</title>
  <link rel="stylesheet" href="./style.css">

</head>
<body>
<!-- partial:index.partial.html -->
<div class="wrapper">
  <div class="candles">
    <div class="light__wave"></div>
    <div class="candle1">
      <div class="candle1__body">
        <div class="candle1__eyes">
          <span class="candle1__eyes-one"></span>
          <span class="candle1__eyes-two"></span>
        </div>
        <div class="candle1__mouth"></div>
      </div>
      <div class="candle1__stick"></div>
    </div>
    
    <div class="candle2">
      <div class="candle2__body">
        <div class="candle2__eyes">
          <div class="candle2__eyes-one"></div>
          <div class="candle2__eyes-two"></div>
        </div>
      </div>
      <div class="candle2__stick"></div>
    </div>
    <div class="candle2__fire"></div>
    <div class="sparkles-one"></div>
    <div class="sparkles-two"></div>
    <div class="candle__smoke-one">

    </div>
    <div class="candle__smoke-two">

    </div>
    
  </div>
  <div class="floor">
  </div>
  

</div>
<!-- partial -->
  
</body>
</html>

```
```css
body {
  background-color: #FFF;
  animation: change-background 3s infinite linear;
}

.wrapper {
  position: absolute;
  left: 50%;
  top: 70%;
  transform: scale(1.5, 1.5) translate(-50%, -50%);
}

.floor {
  position: absolute;
  left: 50%;
  top: 50%;
  width: 350px;
  height: 5px;
  background: #673C63;
  transform: translate(-50%, -50%);
  box-shadow: 0px 2px 5px #111;
  z-index: 2;
}

.candles {
  position: absolute;
  left: 50%;
  top: 50%;
  width: 250px;
  height: 150px;
  transform: translate(-50%, -100%);
  z-index: 1;
}

.candle1 {
  position: absolute;
  left: 50%;
  top: 50%;
  width: 35px;
  height: 100px;
  background: #fff;
  border: 3px solid #673C63;
  border-bottom: 0px;
  border-radius: 3px;
  transform-origin: center right;
  transform: translate(60%, -25%);
  box-shadow: -2px 0px 0px #95c6f2 inset;
  animation: expand-body 3s infinite linear;
}

.candle1__stick, .candle2__stick {
  position: absolute;
  left: 50%;
  top: 0%;
  width: 3px;
  height: 15px;
  background: #673C63;
  border-radius: 8px;
  transform: translate(-50%, -100%);
}

.candle2__stick {
  height: 12px;
  transform-origin: bottom center;
  animation: stick-animation 3s infinite linear;
}

.candle1__eyes, .candle2__eyes {
  position: absolute;
  left: 50%;
  top: 0%;
  width: 35px;
  height: 30px;
  transform: translate(-50%, 0%);
}

.candle1__eyes-one {
  position: absolute;
  left: 30%;
  top: 20%;
  width: 5px;
  height: 5px;
  border-radius: 100%;
  background: #673C63;
  transform: translate(-70%, 0%);
  animation: blink-eyes 3s infinite linear;
}

.candle1__eyes-two {
  position: absolute;
  left: 70%;
  top: 20%;
  width: 5px;
  height: 5px;
  border-radius: 100%;
  background: #673C63;
  transform: translate(-70%, 0%);
  animation: blink-eyes 3s infinite linear;
}

.candle1__mouth {
  position: absolute;
  left: 40%;
  top: 20%;
  width: 0px;
  height: 0px;
  border-radius: 20px;
  background: #673C63;
  transform: translate(-50%, -50%);
  animation: uff 3s infinite linear;
}

.candle__smoke-one {
  position: absolute;
  left: 30%;
  top: 50%;
  width: 30px;
  height: 3px;
  background: grey;
  transform: translate(-50%, -50%);
  animation: move-left 3s infinite linear;
}

.candle__smoke-two {
  position: absolute;
  left: 30%;
  top: 40%;
  width: 10px;
  height: 10px;
  border-radius: 10px;
  background: grey;
  transform: translate(-50%, -50%);
  animation: move-top 3s infinite linear;
}

.candle2 {
  position: absolute;
  left: 20%;
  top: 65%;
  width: 42px;
  height: 60px;
  background: #fff;
  border: 3px solid #673C63;
  border-bottom: 0px;
  border-radius: 3px;
  transform: translate(60%, -15%);
  transform-origin: center right;
  box-shadow: -2px 0px 0px #95c6f2 inset;
  animation: shake-left 3s infinite linear;
}

.candle2__eyes-one {
  position: absolute;
  left: 30%;
  top: 50%;
  width: 5px;
  height: 5px;
  display: inline-block;
  border: 0px solid #673C63;
  border-radius: 100%;
  float: left;
  background: #673C63;
  transform: translate(-80%, 0%);
  animation: changeto-lower 3s infinite linear;
}

.candle2__eyes-two {
  position: absolute;
  left: 70%;
  top: 50%;
  width: 5px;
  height: 5px;
  display: inline-block;
  border: 0px solid #673C63;
  border-radius: 100%;
  float: left;
  background: #673C63;
  transform: translate(-80%, 0%);
  animation: changeto-greater 3s infinite linear;
}

.light__wave {
  position: absolute;
  top: 35%;
  left: 35%;
  width: 75px;
  height: 75px;
  border-radius: 100%;
  z-index: 0;
  transform: translate(-25%, -50%) scale(2.5, 2.5);
  border: 2px solid rgba(255, 255, 255, 0.2);
  animation: expand-light 3s infinite linear;
}

.candle2__fire {
  position: absolute;
  top: 50%;
  left: 40%;
  display: block;
  width: 16px;
  height: 20px;
  background-color: red;
  border-radius: 50% 50% 50% 50% / 60% 60% 40% 40%;
  background: #FF9800;
  transform: translate(-50%, -50%);
  animation: dance-fire 3s infinite linear;
}

@keyframes blink-eyes {
  0%,35% {
    opacity: 1;
    transform: translate(-70%, 0%);
  }
  36%,39% {
    opacity: 0;
    transform: translate(-70%, 0%);
  }
  40% {
    opacity: 1;
    transform: translate(-70%, 0%);
  }
  50%,65% {
    transform: translate(-140%, 0%);
  }
  66% {
    transform: translate(-70%, 0%);
  }
}
@keyframes expand-body {
  0%,40% {
    transform: scale(1, 1) translate(60%, -25%);
  }
  45%,55% {
    transform: scale(1.1, 1.1) translate(60%, -28%);
  }
  60% {
    transform: scale(0.89, 0.89) translate(60%, -25%);
  }
  65% {
    transform: scale(1, 1) translate(60%, -25%);
  }
  70% {
    transform: scale(0.95, 0.95) translate(60%, -25%);
  }
  75% {
    transform: scale(1, 1) translate(60%, -25%);
  }
}
@keyframes uff {
  0%,40% {
    width: 0px;
    height: 0px;
  }
  50%,54% {
    width: 15px;
    height: 15px;
    left: 30%;
  }
  59% {
    width: 5px;
    height: 5px;
    left: 20%;
  }
  62% {
    width: 2px;
    height: 2px;
    left: 20%;
  }
  67% {
    width: 0px;
    height: 0px;
    left: 30%;
  }
}
@keyframes change-background {
  0%,59%,98%,100% {
    background: #FFF;
  }
  61%,97% {
    background: #000;
  }
}
@keyframes move-left {
  0%,59%,100% {
    width: 0px;
    left: 40%;
  }
  60% {
    width: 30px;
    left: 30%;
  }
  68% {
    width: 0px;
    left: 20%;
  }
}
@keyframes move-top {
  0%,64%,100% {
    width: 0px;
    height: 0px;
    top: 0%;
  }
  65% {
    width: 10px;
    height: 10px;
    top: 40%;
    left: 40%;
  }
  80% {
    width: 0px;
    height: 0px;
    top: 20%;
  }
}
@keyframes shake-left {
  0%,40% {
    left: 20%;
    transform: translate(60%, -15%);
  }
  50%,54% {
    left: 20%;
    transform: translate(60%, -15%);
  }
  59% {
    left: 20%;
    transform: translate(60%, -15%);
  }
  62% {
    left: 18%;
    transform: translate(60%, -15%);
  }
  65% {
    left: 21%;
    transform: translate(60%, -15%);
  }
  67% {
    left: 20%;
    transform: translate(60%, -15%);
  }
  75% {
    left: 20%;
    transform: scale(1.15, 0.85) translate(60%, -15%);
    background: #fff;
    border-color: #673C63;
  }
  91% {
    left: 20%;
    transform: scale(1.18, 0.82) translate(60%, -10%);
    background: #F44336;
    border-color: #F44336;
    box-shadow: -2px 0px 0px #F44336 inset;
  }
  92% {
    left: 20%;
    transform: scale(0.85, 1.15) translate(60%, -15%);
  }
  95% {
    left: 20%;
    transform: scale(1.05, 0.95) translate(60%, -15%);
  }
  97% {
    left: 20%;
    transform: scale(1, 1) translate(60%, -15%);
  }
}
@keyframes stick-animation {
  0%,40% {
    left: 50%;
    top: 0%;
    transform: translate(-50%, -100%);
  }
  50%,54% {
    left: 50%;
    top: 0%;
    transform: translate(-50%, -100%);
  }
  59% {
    left: 50%;
    top: 0%;
    transform: translate(-50%, -100%);
  }
  62% {
    left: 50%;
    top: 0%;
    transform: rotateZ(-15deg) translate(-50%, -100%);
  }
  65% {
    left: 50%;
    top: 0%;
    transform: rotateZ(15deg) translate(-50%, -100%);
  }
  70% {
    left: 50%;
    top: 0%;
    transform: rotateZ(-5deg) translate(-50%, -100%);
  }
  72% {
    left: 50%;
    top: 0%;
    transform: rotateZ(5deg) translate(-50%, -100%);
  }
  74%,84% {
    left: 50%;
    top: 0%;
    transform: rotateZ(0deg) translate(-50%, -100%);
  }
  85% {
    transform: rotateZ(180deg) translate(0%, 120%);
  }
  92% {
    left: 50%;
    top: 0%;
    transform: translate(-50%, -100%);
  }
}
@keyframes expand-light {
  10%,29%,59%,89% {
    transform: translate(-25%, -50%) scale(0, 0);
    border: 2px solid rgba(255, 255, 255, 0);
  }
  90%,20%,50% {
    transform: translate(-25%, -50%) scale(1, 1);
  }
  95%,96%,26%,27%,56%,57% {
    transform: translate(-25%, -50%) scale(2, 2);
    border: 2px solid rgba(255, 255, 255, 0.5);
  }
  0%,28%,58%,100% {
    transform: translate(-25%, -50%) scale(2.5, 2.5);
    border: 2px solid rgba(255, 255, 255, 0.2);
  }
}
@keyframes dance-fire {
  59%,89% {
    left: 40%;
    width: 0px;
    height: 0px;
  }
  90%,0%,7%,15%,23%,31%,39%,47%,55% {
    left: 40.8%;
    width: 16px;
    height: 20px;
    background: #FFC107;
  }
  94%,3%,11%,19%,27%,35%,43%,51%,58% {
    left: 41.2%;
    width: 16px;
    height: 20px;
    background: #FF9800;
  }
}
@keyframes changeto-lower {
  0%,70%,90% {
    padding: 0px;
    display: inline-block;
    border-radius: 100%;
    background: #673C63;
    border-width: 0 0 0 0;
    border: 0px solid #673C63;
    transform: translate(-90%, 0%);
  }
  71%,89% {
    background: none;
    border: solid #673C63;
    border-radius: 0px;
    border-width: 0 2px 2px 0;
    display: inline-block;
    padding: 1px;
    float: left;
    transform-origin: bottom left;
    transform: rotate(-45deg) translate(-50%, -65%);
    -webkit-transform: rotate(-45deg) translate(-50%, -65%);
  }
}
@keyframes changeto-greater {
  0%,70%,90% {
    top: 50%;
    padding: 0px;
    display: inline-block;
    border-radius: 100%;
    background: #673C63;
    border-width: 0 0 0 0;
    border: 0px solid #673C63;
    transform: translate(-80%, 0%);
  }
  71%,89% {
    top: 30%;
    background: none;
    border: solid #673C63;
    border-radius: 0px;
    border-width: 0 2px 2px 0;
    display: inline-block;
    padding: 1px;
    float: left;
    transform-origin: bottom left;
    transform: rotate(135deg) translate(-80%, 20%);
    -webkit-transform: rotate(135deg) translate(-80%, 20%);
  }
}
```

===


37. Ants on sugar

Ants on Sugar CSS Animation

The ants on sugar animation by Stephen Fairbanks uses various keyframe animations to create ants moving across a cube of sugar and the background in general.
```html
<div class="wrapper">
  <div class="cube">
    <h1>Sugar Cube</h1>
    <svg id="ant1" class="ants" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 29.39 53.12"><title>ant</title><path d="M539.22,398.8a20.17,20.17,0,0,0,3-10.91,14,14,0,0,1,6.22-12.12c.58-.41.56-1.79.6-2.74,0-.18-1-.44-1.53-.61a10.21,10.21,0,0,1-2.52-.74,3.21,3.21,0,0,0-3.17-.18,16.22,16.22,0,0,1-5,.34c-.4,0-.8-.54-1.2-.83l.17-.32c.3.09.6.16.89.27a4.36,4.36,0,0,0,4.08-.16c1.9-1.12,3.92-1.5,5.88.08a8.9,8.9,0,0,0,1.79.86c.43-2.45.07-4.19-1.94-5.61-1.13-.8-1.77-2.3-2.59-3.51a4.82,4.82,0,0,1-.58-1.2c-.89-2.69-3.22-3.93-5.38-5.33a8.47,8.47,0,0,1-.86-.64c-.09-.07-.11-.23-.32-.71.74.37,1.22.58,1.67.83,1.32.74,2.66,1.45,3.92,2.28a3.13,3.13,0,0,1,1,1.45,13.54,13.54,0,0,0,4.09,6l1.88-2.86c-2.65-.24-3.22-1.08-2.51-3.8.22-.86.56-1.68.91-2.71-1.3-.4-2.55-.85-3.83-1.17a3,3,0,0,1-2.21-1.94c-1-2.31-1.82-4.69-3.92-6.3-.13-.1-.11-.38.08-.83.42.31,1,.52,1.24.94,1,1.66,1.92,3.41,3,5.06a5.58,5.58,0,0,0,1.66,1.73,19.86,19.86,0,0,0,2.81,1.25c1,.44,2,.88,3.13.07a1.68,1.68,0,0,1,1.42-.13c1.29.57,2.27-.17,3.4-.52,2.45-.75,4.18-2,4.47-4.76a3.2,3.2,0,0,1,2.11-2.53,3.33,3.33,0,0,1-.26.82,9.36,9.36,0,0,0-2,5,1.67,1.67,0,0,1-1.59,1.65c-1.33.26-2.62.7-3.87,1a38.19,38.19,0,0,1,1.09,4c.36,2.31-.35,3.06-2.8,3l1.91,3a46.1,46.1,0,0,0,3-4.59c.74-1.48,1.2-2.93,3-3.58,1.38-.49,2.57-1.55,4.06-2.15a2.18,2.18,0,0,1-.39.65,8.37,8.37,0,0,1-1.66,1.13c-2.62,1.17-3.78,3.5-4.92,5.91a11.72,11.72,0,0,1-2.19,3.19c-2.42,2.44-2.18,2.73-2,5.77a7.21,7.21,0,0,0,1.52-.72c1.88-1.54,3.86-1.32,5.75-.2a4.4,4.4,0,0,0,4.45.1c.2-.09.41-.15.61-.22l.26.36c-.55.34-1.1,1-1.66,1-1.8,0-3.66.35-5.35-.72a2.33,2.33,0,0,0-1.56.07c-1.22.33-2.43.73-3.63,1.13a1.85,1.85,0,0,0-.86.47c-.28.35.41,3,.78,3.36.79.72,1.59,1.43,2.36,2.18,2.78,2.68,3.25,6.16,3.36,9.75a22.41,22.41,0,0,0,2.87,10.44,1.74,1.74,0,0,1-1.64-1.54c-.86-3.41-2.14-6.72-2.29-10.31-.14-3.25-.56-4.16-1.89-5.52a24.93,24.93,0,0,1-.17,4.45,12.52,12.52,0,0,1-1.68,4.17c-1.53,2.36-4.41,2.28-6-.05-1.85-2.67-2.05-5.66-1.53-8.76.06-.35.16-.69.25-1.07a6.81,6.81,0,0,0-2.94,5.69c-.17,3.81-1.31,7.38-2.26,11A2.54,2.54,0,0,1,539.22,398.8Z" transform="translate(-535.68 -345.68)"/></svg>
    <svg id="ant2" class="ants" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 29.39 53.12"><title>ant</title><path d="M539.22,398.8a20.17,20.17,0,0,0,3-10.91,14,14,0,0,1,6.22-12.12c.58-.41.56-1.79.6-2.74,0-.18-1-.44-1.53-.61a10.21,10.21,0,0,1-2.52-.74,3.21,3.21,0,0,0-3.17-.18,16.22,16.22,0,0,1-5,.34c-.4,0-.8-.54-1.2-.83l.17-.32c.3.09.6.16.89.27a4.36,4.36,0,0,0,4.08-.16c1.9-1.12,3.92-1.5,5.88.08a8.9,8.9,0,0,0,1.79.86c.43-2.45.07-4.19-1.94-5.61-1.13-.8-1.77-2.3-2.59-3.51a4.82,4.82,0,0,1-.58-1.2c-.89-2.69-3.22-3.93-5.38-5.33a8.47,8.47,0,0,1-.86-.64c-.09-.07-.11-.23-.32-.71.74.37,1.22.58,1.67.83,1.32.74,2.66,1.45,3.92,2.28a3.13,3.13,0,0,1,1,1.45,13.54,13.54,0,0,0,4.09,6l1.88-2.86c-2.65-.24-3.22-1.08-2.51-3.8.22-.86.56-1.68.91-2.71-1.3-.4-2.55-.85-3.83-1.17a3,3,0,0,1-2.21-1.94c-1-2.31-1.82-4.69-3.92-6.3-.13-.1-.11-.38.08-.83.42.31,1,.52,1.24.94,1,1.66,1.92,3.41,3,5.06a5.58,5.58,0,0,0,1.66,1.73,19.86,19.86,0,0,0,2.81,1.25c1,.44,2,.88,3.13.07a1.68,1.68,0,0,1,1.42-.13c1.29.57,2.27-.17,3.4-.52,2.45-.75,4.18-2,4.47-4.76a3.2,3.2,0,0,1,2.11-2.53,3.33,3.33,0,0,1-.26.82,9.36,9.36,0,0,0-2,5,1.67,1.67,0,0,1-1.59,1.65c-1.33.26-2.62.7-3.87,1a38.19,38.19,0,0,1,1.09,4c.36,2.31-.35,3.06-2.8,3l1.91,3a46.1,46.1,0,0,0,3-4.59c.74-1.48,1.2-2.93,3-3.58,1.38-.49,2.57-1.55,4.06-2.15a2.18,2.18,0,0,1-.39.65,8.37,8.37,0,0,1-1.66,1.13c-2.62,1.17-3.78,3.5-4.92,5.91a11.72,11.72,0,0,1-2.19,3.19c-2.42,2.44-2.18,2.73-2,5.77a7.21,7.21,0,0,0,1.52-.72c1.88-1.54,3.86-1.32,5.75-.2a4.4,4.4,0,0,0,4.45.1c.2-.09.41-.15.61-.22l.26.36c-.55.34-1.1,1-1.66,1-1.8,0-3.66.35-5.35-.72a2.33,2.33,0,0,0-1.56.07c-1.22.33-2.43.73-3.63,1.13a1.85,1.85,0,0,0-.86.47c-.28.35.41,3,.78,3.36.79.72,1.59,1.43,2.36,2.18,2.78,2.68,3.25,6.16,3.36,9.75a22.41,22.41,0,0,0,2.87,10.44,1.74,1.74,0,0,1-1.64-1.54c-.86-3.41-2.14-6.72-2.29-10.31-.14-3.25-.56-4.16-1.89-5.52a24.93,24.93,0,0,1-.17,4.45,12.52,12.52,0,0,1-1.68,4.17c-1.53,2.36-4.41,2.28-6-.05-1.85-2.67-2.05-5.66-1.53-8.76.06-.35.16-.69.25-1.07a6.81,6.81,0,0,0-2.94,5.69c-.17,3.81-1.31,7.38-2.26,11A2.54,2.54,0,0,1,539.22,398.8Z" transform="translate(-535.68 -345.68)"/></svg>
    <svg id="ant3" class="ants" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 29.39 53.12"><title>ant</title><path d="M539.22,398.8a20.17,20.17,0,0,0,3-10.91,14,14,0,0,1,6.22-12.12c.58-.41.56-1.79.6-2.74,0-.18-1-.44-1.53-.61a10.21,10.21,0,0,1-2.52-.74,3.21,3.21,0,0,0-3.17-.18,16.22,16.22,0,0,1-5,.34c-.4,0-.8-.54-1.2-.83l.17-.32c.3.09.6.16.89.27a4.36,4.36,0,0,0,4.08-.16c1.9-1.12,3.92-1.5,5.88.08a8.9,8.9,0,0,0,1.79.86c.43-2.45.07-4.19-1.94-5.61-1.13-.8-1.77-2.3-2.59-3.51a4.82,4.82,0,0,1-.58-1.2c-.89-2.69-3.22-3.93-5.38-5.33a8.47,8.47,0,0,1-.86-.64c-.09-.07-.11-.23-.32-.71.74.37,1.22.58,1.67.83,1.32.74,2.66,1.45,3.92,2.28a3.13,3.13,0,0,1,1,1.45,13.54,13.54,0,0,0,4.09,6l1.88-2.86c-2.65-.24-3.22-1.08-2.51-3.8.22-.86.56-1.68.91-2.71-1.3-.4-2.55-.85-3.83-1.17a3,3,0,0,1-2.21-1.94c-1-2.31-1.82-4.69-3.92-6.3-.13-.1-.11-.38.08-.83.42.31,1,.52,1.24.94,1,1.66,1.92,3.41,3,5.06a5.58,5.58,0,0,0,1.66,1.73,19.86,19.86,0,0,0,2.81,1.25c1,.44,2,.88,3.13.07a1.68,1.68,0,0,1,1.42-.13c1.29.57,2.27-.17,3.4-.52,2.45-.75,4.18-2,4.47-4.76a3.2,3.2,0,0,1,2.11-2.53,3.33,3.33,0,0,1-.26.82,9.36,9.36,0,0,0-2,5,1.67,1.67,0,0,1-1.59,1.65c-1.33.26-2.62.7-3.87,1a38.19,38.19,0,0,1,1.09,4c.36,2.31-.35,3.06-2.8,3l1.91,3a46.1,46.1,0,0,0,3-4.59c.74-1.48,1.2-2.93,3-3.58,1.38-.49,2.57-1.55,4.06-2.15a2.18,2.18,0,0,1-.39.65,8.37,8.37,0,0,1-1.66,1.13c-2.62,1.17-3.78,3.5-4.92,5.91a11.72,11.72,0,0,1-2.19,3.19c-2.42,2.44-2.18,2.73-2,5.77a7.21,7.21,0,0,0,1.52-.72c1.88-1.54,3.86-1.32,5.75-.2a4.4,4.4,0,0,0,4.45.1c.2-.09.41-.15.61-.22l.26.36c-.55.34-1.1,1-1.66,1-1.8,0-3.66.35-5.35-.72a2.33,2.33,0,0,0-1.56.07c-1.22.33-2.43.73-3.63,1.13a1.85,1.85,0,0,0-.86.47c-.28.35.41,3,.78,3.36.79.72,1.59,1.43,2.36,2.18,2.78,2.68,3.25,6.16,3.36,9.75a22.41,22.41,0,0,0,2.87,10.44,1.74,1.74,0,0,1-1.64-1.54c-.86-3.41-2.14-6.72-2.29-10.31-.14-3.25-.56-4.16-1.89-5.52a24.93,24.93,0,0,1-.17,4.45,12.52,12.52,0,0,1-1.68,4.17c-1.53,2.36-4.41,2.28-6-.05-1.85-2.67-2.05-5.66-1.53-8.76.06-.35.16-.69.25-1.07a6.81,6.81,0,0,0-2.94,5.69c-.17,3.81-1.31,7.38-2.26,11A2.54,2.54,0,0,1,539.22,398.8Z" transform="translate(-535.68 -345.68)"/></svg>
    <svg id="ant4" class="ants" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 29.39 53.12"><title>ant</title><path d="M539.22,398.8a20.17,20.17,0,0,0,3-10.91,14,14,0,0,1,6.22-12.12c.58-.41.56-1.79.6-2.74,0-.18-1-.44-1.53-.61a10.21,10.21,0,0,1-2.52-.74,3.21,3.21,0,0,0-3.17-.18,16.22,16.22,0,0,1-5,.34c-.4,0-.8-.54-1.2-.83l.17-.32c.3.09.6.16.89.27a4.36,4.36,0,0,0,4.08-.16c1.9-1.12,3.92-1.5,5.88.08a8.9,8.9,0,0,0,1.79.86c.43-2.45.07-4.19-1.94-5.61-1.13-.8-1.77-2.3-2.59-3.51a4.82,4.82,0,0,1-.58-1.2c-.89-2.69-3.22-3.93-5.38-5.33a8.47,8.47,0,0,1-.86-.64c-.09-.07-.11-.23-.32-.71.74.37,1.22.58,1.67.83,1.32.74,2.66,1.45,3.92,2.28a3.13,3.13,0,0,1,1,1.45,13.54,13.54,0,0,0,4.09,6l1.88-2.86c-2.65-.24-3.22-1.08-2.51-3.8.22-.86.56-1.68.91-2.71-1.3-.4-2.55-.85-3.83-1.17a3,3,0,0,1-2.21-1.94c-1-2.31-1.82-4.69-3.92-6.3-.13-.1-.11-.38.08-.83.42.31,1,.52,1.24.94,1,1.66,1.92,3.41,3,5.06a5.58,5.58,0,0,0,1.66,1.73,19.86,19.86,0,0,0,2.81,1.25c1,.44,2,.88,3.13.07a1.68,1.68,0,0,1,1.42-.13c1.29.57,2.27-.17,3.4-.52,2.45-.75,4.18-2,4.47-4.76a3.2,3.2,0,0,1,2.11-2.53,3.33,3.33,0,0,1-.26.82,9.36,9.36,0,0,0-2,5,1.67,1.67,0,0,1-1.59,1.65c-1.33.26-2.62.7-3.87,1a38.19,38.19,0,0,1,1.09,4c.36,2.31-.35,3.06-2.8,3l1.91,3a46.1,46.1,0,0,0,3-4.59c.74-1.48,1.2-2.93,3-3.58,1.38-.49,2.57-1.55,4.06-2.15a2.18,2.18,0,0,1-.39.65,8.37,8.37,0,0,1-1.66,1.13c-2.62,1.17-3.78,3.5-4.92,5.91a11.72,11.72,0,0,1-2.19,3.19c-2.42,2.44-2.18,2.73-2,5.77a7.21,7.21,0,0,0,1.52-.72c1.88-1.54,3.86-1.32,5.75-.2a4.4,4.4,0,0,0,4.45.1c.2-.09.41-.15.61-.22l.26.36c-.55.34-1.1,1-1.66,1-1.8,0-3.66.35-5.35-.72a2.33,2.33,0,0,0-1.56.07c-1.22.33-2.43.73-3.63,1.13a1.85,1.85,0,0,0-.86.47c-.28.35.41,3,.78,3.36.79.72,1.59,1.43,2.36,2.18,2.78,2.68,3.25,6.16,3.36,9.75a22.41,22.41,0,0,0,2.87,10.44,1.74,1.74,0,0,1-1.64-1.54c-.86-3.41-2.14-6.72-2.29-10.31-.14-3.25-.56-4.16-1.89-5.52a24.93,24.93,0,0,1-.17,4.45,12.52,12.52,0,0,1-1.68,4.17c-1.53,2.36-4.41,2.28-6-.05-1.85-2.67-2.05-5.66-1.53-8.76.06-.35.16-.69.25-1.07a6.81,6.81,0,0,0-2.94,5.69c-.17,3.81-1.31,7.38-2.26,11A2.54,2.54,0,0,1,539.22,398.8Z" transform="translate(-535.68 -345.68)"/></svg>
  </div>
</div>
```
```css
body {
  overflow: hidden;
  margin: 0;
  padding: 0;
}

.wrapper {
  display: flex;
  width: 100vw;
  height: 100vh;
  align-items: center;
  justify-content: center;
  background: #7ae298;
  /* Old browsers */
  background: -moz-linear-gradient(45deg, #7ae298 0%, #9af282 100%);
  /* FF3.6-15 */
  background: -webkit-linear-gradient(45deg, #7ae298 0%, #9af282 100%);
  /* Chrome10-25,Safari5.1-6 */
  background: linear-gradient(45deg, #7ae298 0%, #9af282 100%);
}

.cube {
  position: relative;
  font-family: 'Oswald', sans-serif;
  width: 250px;
  height: 250px;
  box-shadow: 30px 30px 20px rgba(0, 0, 0, 0.1);
  background-color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
}

.ants {
  fill: black;
  width: 15px;
  height: 25px;
  position: absolute;
}

#ant1 {
  left: 50%;
  bottom: -60%;
  animation: ant-1-animation 8s infinite;
}

#ant2 {
  left: 0;
  bottom: -50%;
  animation: ant-2-animation 9s infinite;
}

#ant3 {
  left: 100%;
  bottom: -70%;
  animation: ant-3-animation 12s infinite;
  animation-delay: 1s;
}

#ant4 {
  left: 0;
  bottom: -50%;
  animation: ant-2-animation 12s infinite;
  animation-delay: 3s;
}

// ANT-IMATIONS
@keyframes ant-1-animation {
  0% {
    left: 50%;
    bottom: -60%;
  }
  10% {
    left: 50%;
    bottom: 5%;
    transform: rotate(0deg);
  }
  12% {
    left: 50%;
    bottom: 5%;
    transform: rotate(-90deg);
  }
  18% {
    transform: rotate(-90deg);
  }
  20% {
    left: 5%;
    bottom: 5%;
    transform: rotate(-90deg);
  }
  22% {
    transform: rotate(0);
  }
  32% {
    transform: rotate(0);
  }
  38% {
    left: 5%;
    bottom: 87%;
    transform: rotate(90deg);
  }
  50% {
    left: 90%;
    bottom: 87%;
    transform: rotate(90deg);
  }
  55% {
    left: 90%;
    bottom: 87%;
    transform: rotate(230deg);
  }
  72% {
    left: 5%;
    bottom: 5%;
    transform: rotate(230deg);
  }
  78% {
    left: 5%;
    bottom: 5%;
    transform: rotate(90deg);
  }
  85% {
    left: 50%;
    bottom: 5%;
    transform: rotate(90deg);
  }
  90% {
    left: 50%;
    bottom: 5%;
    transform: rotate(180deg);
  }
  100% {
    left: 50%;
    bottom: -60%;
    transform: rotate(180deg);
  }
}

// ANT 2
@keyframes ant-2-animation {
  0% {
    left: 0;
    bottom: -50%;
    transform: rotate(45deg);
  }
  15% {
    left: 50%;
    bottom: 5%;
    transform: rotate(45deg);
  }
  18% {
    left: 50%;
    bottom: 5%;
    transform: rotate(90deg);
  }
  28% {
    left: 85%;
    bottom: 5%;
    transform: rotate(90deg);
  }
  30% {
    left: 85%;
    bottom: 5%;
    transform: rotate(0deg);
  }
  40% {
    left: 85%;
    bottom: 85%;
    transform: rotate(0deg);
  }
  48% {
    left: 85%;
    bottom: 85%;
    transform: rotate(-90deg);
  }
  58% {
    left: 5%;
    bottom: 85%;
    transform: rotate(-90deg);
  }
  68% {
    left: 5%;
    bottom: 85%;
    transform: rotate(-180deg);
  }
  79% {
    left: 5%;
    bottom: 5%;
    transform: rotate(-180deg);
  }
  81% {
    left: 5%;
    bottom: 5%;
    transform: rotate(-270deg);
  }
  88% {
    left: 50%;
    bottom: 5%;
    transform: rotate(-270deg);
  }
  90% {
    left: 50%;
    bottom: 5%;
    transform: rotate(-140deg);
  }
  100% {
    left: 0%;
    bottom: -50%;
    transform: rotate(-140deg);
  }
}

// ANT 3
@keyframes ant-3-animation {
  0% {
    left: 100%;
    bottom: -70%;
    transform: rotate(-45deg);
  }
  15% {
    left: 50%;
    bottom: -15%;
    transform: rotate(-45deg);
  }
  18% {
    left: 50%;
    bottom: -15%;
    transform: rotate(-90deg);
  }
  28% {
    left: -10%;
    bottom: -15%;
    transform: rotate(-90deg);
  }
  30% {
    left: -10%;
    bottom: -15%;
    transform: rotate(0deg);
  }
  45% {
    left: -10%;
    bottom: 105%;
    transform: rotate(0deg);
  }
  48% {
    left: -10%;
    bottom: 105%;
    transform: rotate(90deg);
  }
  58% {
    left: 105%;
    bottom: 105%;
    transform: rotate(90deg);
  }
  60% {
    left: 105%;
    bottom: 105%;
    transform: rotate(180deg);
  }
  75% {
    left: 105%;
    bottom: -15%;
    transform: rotate(180deg);
  }
  81% {
    left: 105%;
    bottom: -15%;
    transform: rotate(270deg);
  }
  88% {
    left: 50%;
    bottom: -15%;
    transform: rotate(270deg);
  }
  90% {
    left: 50%;
    bottom: -15%;
    transform: rotate(180deg);
  }
  92% {
    left: 50%;
    bottom: -15%;
    transform: rotate(130deg);
  }
  100% {
    left: 100%;
    bottom: -70%;
    transform: rotate(130deg);
  }
}
```
```javascript
```
===


38. Submarine

Submarine with CSS

The submarine animation by Alberto Jerez creates a submarine immersed in a body of water. It defines various keyframes that handle the animation of the bubbles, lights, and the submarine’s body and blades.

```html
<div class="sea">
    <div class="circle-wrapper">
        <div class="bubble"></div>
        <div class="submarine-wrapper">
            <div class="submarine-body">
                <div class="window"></div>
                <div class="engine"></div>
                <div class="light"></div>
            </div>
            <div class="helix"></div>
            <div class="hat">
              <div class="leds-wrapper">
                  <div class="periscope"></div>
                  <div class="leds"></div>
              </div>
            </div>
        </div>
    </div>
</div>




<!-- Link to my website -->
<a id="ajerez" href="http://www.ajerez.es/en/" target="_blank"><img src="https://i.imgur.com/AI4BS2I.png"/></a>
```
```scss


$color1: #306D85;
$color2: #D93A54;

body {
    background-color: $color1;
    

}

*, *:before, *:after {
    box-sizing: border-box;
}


.sea {
    margin: 40px auto 0 auto;
    overflow: hidden;
    
    .bubble {
        position: absolute;
        width: 7px;
        height: 7px;
        border-radius: 50%;
        background-color: lighten($color1, 25%);
        opacity: 0.9;
        animation: bubble1-h-movement 1s ease-in infinite, bubble1-v-movement 300ms ease-in-out infinite alternate, bubble-scale-movement 300ms ease-in-out infinite alternate;
        
        &:after {
            position: absolute;
            content: "";
            width: 7px;
            height: 7px;
            border-radius: 50%;
            background-color: lighten($color1, 25%);
            opacity: 0.9;
            
        }
        &:after {
          top: -20;
          left:100px;
          width: 9px;
          height: 9px;
        }
        

    }
    
    .circle-wrapper {
        position: relative;
        background: linear-gradient(darken($color1, 3%), darken($color1, 12%));
        width:300px;
        height:300px;
        margin: 10px auto 0 auto;
        overflow: hidden;
        z-index: 0;
        border-radius:50%;
        padding: 0 50px 0 50px;
        border: 6px solid lighten($color1, 10%);
        
    }
    
    .submarine-wrapper {
        height: 300px;
        width: 300px;
        padding: 30px 50px 30px 150px;
        margin: 0 auto 0 auto;
        animation: diving 3s ease-in-out infinite, diving-rotate 3s ease-in-out infinite;
        
       .submarine-body {
           
           width: 150px;
           height: 80px;
           position: absolute;
           margin-top: 50px;
           left: 25px;
           background-color: $color2;
           border-radius: 40px;
           background: linear-gradient($color2, darken($color2, 10%));
           
           .light {
                position:absolute;
                width: 0;
                height: 0;
                border-style: solid;
                border-width: 0 40px 150px 40px;
                border-color: transparent transparent lighten($color1,5%) transparent;
               
                transform: rotate(-50deg);
               top:40px;
               left:99%;
               
               &:after {
                   content:"";
                   position: absolute;
                   width: 20px;
                   height:13px;
                   border-radius:5px;
                   background-color:darken($color2, 5%);
                   margin-left:-10px;
               }

           }
           
           .window {
               width: 37px;
               height: 37px;
               position: absolute;
               margin-top: 23px;
               right: 18px;
               background: linear-gradient(darken($color1, 13%), darken($color1, 18%));
               border-radius: 50%;
               border: 3px solid $color2;
               
                &:after {
                  content: "";
                  position: absolute;
                  margin-top:3px;
                  margin-left:3px;
                  width: 25px;
                  height: 25px;
                  border-radius: 50%;
                  background-color: transparent;
                  opacity:0.8;
                  border-top:3px solid white;
                  transform:rotate(-45deg); 
                }
           }
           
           .engine {
               width: 30px;
               height: 30px;
               position: absolute;
               margin-top: 32px;
               left: 53px;
               background-color: darken($color2, 10%);
               border-radius: 50%;
               border: 5px solid $color2;
               
               &:after, &:before {
                  position: absolute;
                  content: "";
                  border-radius: 2px;
                  background-color: white;
                  animation:spin 900ms linear infinite;
                  opacity:0.8;
                }
                &:after {
                  top:8px;
                  width: 20px;
                  height: 4px;
                  
                }
                &:before {
                  left:8px;
                  width: 4px;
                  height: 20px;
                  
                }
           }
       }
       
        .helix {
           width: 30px;
           height: 70px;
           position: absolute;
           margin-top: 55px;
           left:0;
           background-color: $color2;
           border-radius: 7px;
           background: linear-gradient($color2, darken($color2, 10%));
           
            &:after {
                content: "";
                position: absolute;
                margin-top:5px;
                margin-left:7px;
                width: 17px;
                height: 60px;
                border-radius: 3px;
                background-color: transparent;
                opacity:0.8;
                background: linear-gradient(
                  to bottom,
                  $color2,
                  $color2 50%,
                  lighten($color2, 15%) 50%,
                  lighten($color2, 15%)
                );
                background-size: 100% 20px;
                animation:helix-movement 110ms linear infinite;

            }
        }
        
        
        .hat {
           width: 65px;
           height: 25px;
           position: absolute;
           margin-top: 26px;
           left:70px;
           background-color: $color2;
           border-radius: 10px 10px 0 0;
           background: linear-gradient($color2, darken($color2, 3%));
           
            .periscope {
                position: absolute;
                width: 7px;
                height: 20px;
                background-color: $color2;
                margin-top: -27px;
                margin-left:32px;
                border-radius:5px 5px 0 0;
                
                &:after, &:before {
                    content: "";
                    position: absolute;
                    width: 15px;
                    height: 7px;
                    border-radius: 5px;
                    background-color: $color2;
                }
            }
            
            .leds-wrapper {
               width: 53px;
               height: 13px;
               position: relative;
               top:7px;
               left:7px;
               background-color: $color2;
               border-radius: 10px;
               background: linear-gradient(darken($color2, 12%), darken($color2, 16%));
                
                .leds {
                    position: absolute;
                    margin-top:4px;
                    margin-left:7px;
                    width: 5px;
                    height: 5px;
                    border-radius: 50%;
                    background-color: white;
                    animation:leds-off 500ms linear infinite;

                    &:after, &:before {
                        content: "";
                        position: absolute;
                        width: 5px;
                        height: 5px;
                        border-radius: 50%;
                        background-color: white;
                    }
                    &:after {
                        margin-top:0px;
                        margin-left:17px;
                    }
                    &:before {
                        margin-top:0px;
                        margin-left:34px;
                    }
                }
            }
        }
        
        
    }
}


@keyframes spin { 
    100% { 
        transform:rotate(360deg); 
    } 
}

@keyframes leds-off { 
    100% { 
        opacity:0.3;
    } 
}

@keyframes helix-movement {
    100% {
        background: linear-gradient(
          to bottom,
          lighten($color2, 15%) 50%,
          lighten($color2, 15%),
          $color2,
          $color2 50%
        );
        background-size: 100% 20px;
    }
}


@keyframes diving {
	0% {
         margin-top:5px;
	}
    50% {
		 margin-top:15px;
	}
    
    100% {
		 margin-top:5px;
	}
}

@keyframes diving-rotate {
	0% {
         transform:rotate(0deg); 
	}
    50% {
         transform:rotate(3deg); 
	}
    75% {
        transform:rotate(-2deg); 
	}
    100% {
        transform:rotate(0deg); 
	}
}

    @keyframes bubble1-h-movement {
	  0% { 
          margin-left: 80%;
      }
	  100% { 
           margin-left: -100%; 
      }
	}	

    @keyframes bubble2-h-movement {
	  0% { 
          margin-left: 65%;
      }
	  100% { 
           margin-left: -5%; 
      }
	}

	@keyframes bubble1-v-movement {
	  0% { 
          margin-top: 115px; 
      }
	  100% { 
          margin-top:160px; 
      }
	}
    
    @keyframes bubble2-v-movement {
	  0% { 
          margin-top: 115px; 
      }
	  100% { 
          margin-top:90px; 
      }
	}

    @keyframes bubble-scale-movement {
	  0% { 
          transform: scale(1.4);
      }
	  100% { 
          transform: scale(0.9);
      }
	}

    @keyframes light-movement {
	  0% { 
          transform: rotate(-40deg);
      }
      50% {
          transform: rotate(-70deg);
      }
	  100% { 
          transform: rotate(-40deg);
      }
	}


```

===

39. Glowing loader
```html
<div class="animation-container">
	<div class="lightning-container">
		<div class="lightning white"></div>
		<div class="lightning red"></div>
	</div>
	<div class="boom-container">
		<div class="shape circle big white"></div>
		<div class="shape circle white"></div>
		<div class="shape triangle big yellow"></div>
		<div class="shape disc white"></div>
		<div class="shape triangle blue"></div>
	</div>
	<div class="boom-container second">
		<div class="shape circle big white"></div>
		<div class="shape circle white"></div>
		<div class="shape disc white"></div>
		<div class="shape triangle blue"></div>
	</div>
</div>

<div class="footer">Implemented with ❤ by <a href="https://mrossignol.fr" target="_blank">Maxime Rossignol</a> from an original idea by <a href="https://www.uplabs.com/shashanksahay" traget="_blank">Shashank Sahay</a></div>
```
```scss
body {
	display: flex;
	align-items: center;
	position: relative;
	background: linear-gradient(to bottom right, #070630 0%,#060454 100%);
	min-height: 100vh;
}

.animation-container {
	display: block;
	position: relative;
	width: 800px;
	max-width: 100%;
	margin: 0 auto;
	
	.lightning-container {
		position: absolute;
		top: 50%;
		left: 0;
		display: flex;
		transform: translateY(-50%);
		
		.lightning {
			position: absolute;
			display: block;
			height: 12px;
			width: 12px;
			border-radius: 12px;
			transform-origin: 6px 6px;

			animation-name: woosh;
			animation-duration: 1.5s;
			animation-iteration-count: infinite;
			animation-timing-function: cubic-bezier(0.445, 0.050, 0.550, 0.950);
			animation-direction: alternate;

			&.white {
				background-color: white;
				box-shadow: 0px 50px 50px 0px transparentize(white, 0.7);
			}

			&.red {
				background-color: #fc7171;
				box-shadow: 0px 50px 50px 0px transparentize(#fc7171, 0.7);
				animation-delay: 0.2s;
			}
		}
	}
	
	
	.boom-container {
		position: absolute;
		display: flex;
		width: 80px;
		height: 80px;
		text-align: center;
		align-items: center;
		transform: translateY(-50%);
    left: 200px;
    top: -145px;
		
		.shape {
			display: inline-block;
			position: relative;
			opacity: 0;
			transform-origin: center center;
			
			&.triangle {
				width: 0;
				height: 0;
				border-style: solid;
				transform-origin: 50% 80%;
				animation-duration: 1s;
				animation-timing-function: ease-out;
				animation-iteration-count: infinite;
				margin-left: -15px;
				border-width: 0 2.5px 5px 2.5px;
				border-color: transparent transparent #42e599 transparent;
				animation-name: boom-triangle;
				
				&.big {
					margin-left: -25px;
					border-width: 0 5px 10px 5px;
					border-color: transparent transparent #fade28 transparent;
					animation-name: boom-triangle-big;
				}
			}
			
			&.disc {
				width: 8px;
				height: 8px;
				border-radius: 100%;
				background-color: #d15ff4;
				animation-name: boom-disc;
				animation-duration: 1s;
				animation-timing-function: ease-out;
				animation-iteration-count: infinite;
			}
			
			&.circle {
				width: 20px;
				height: 20px;
				animation-name: boom-circle;
				animation-duration: 1s;
				animation-timing-function: ease-out;
				animation-iteration-count: infinite;
				border-radius: 100%;
				margin-left: -30px;
				
				&.white {
					border: 1px solid white;
				}
				
				&.big {
					width: 40px;
					height: 40px;
					margin-left: 0px;
					
					&.white {
						border: 2px solid white;
					}
				}
			}
			
			&:after {
				background-color: rgba(178, 215, 232, 0.2);
			}
		}
		
		.shape {
			&.triangle, &.circle, &.circle.big, &.disc {
				animation-delay: .38s;
				animation-duration: 3s;
			}
			
			&.circle {
				animation-delay: 0.6s;
			}
		}
		
		&.second {
			left: 485px;
			top: 155px;
			.shape {
				&.triangle, &.circle, &.circle.big, &.disc {
					animation-delay: 1.9s;
				}
				&.circle {
					animation-delay: 2.15s;
				}
			}
		}
	}
}

@keyframes woosh {
	0% {
		width: 12px;
		transform: translate(0px, 0px) rotate(-35deg);
	}
	15% {
		width: 50px;
	}
	30% {
		width: 12px;
		transform: translate(214px, -150px) rotate(-35deg);
	}
	30.1% {
		transform: translate(214px, -150px) rotate(46deg);
	}
	50% {
		width: 110px;
	}
	70% {
		width: 12px;
		transform: translate(500px, 150px) rotate(46deg);
	}
	70.1% {
		transform: translate(500px, 150px) rotate(-37deg);
	}
	
	85% {
		width: 50px;
	}
	100% {
		width: 12px;
		transform: translate(700px, 0) rotate(-37deg);
	}
}

@keyframes boom-circle {
	0% {
		opacity: 0;
	}
	5% {
		opacity: 1;
	}
	30% {
		opacity: 0;
		transform: scale(3);
	}
	100% {
	}
}

@keyframes boom-triangle-big {
	0% {
		opacity: 0;
	}
	5% {
		opacity: 1;
	}
	
	40% {
		opacity: 0;
		transform: scale(2.5) translate(50px, -50px) rotate(360deg);
	}
	100% {
	}
}

@keyframes boom-triangle {
	0% {
		opacity: 0;
	}
	5% {
		opacity: 1;
	}
	
	30% {
		opacity: 0;
		transform: scale(3) translate(20px, 40px) rotate(360deg);
	}
	
	100% {
	}
}

@keyframes boom-disc {
	0% {
		opacity: 0;
	}
	5% {
		opacity: 1;
	}
	40% {
		opacity: 0;
		transform: scale(2) translate(-70px, -30px);
	}
	100% {
		
	}
}


// FOOTER
.footer {
	color: white;
	font-size: 10px;
	position: fixed;
	bottom: 0;
	font-weight: 200;
	padding: 10px 20px;
	
	a {
		&,
		&:hover,
		&:focus,
		&:visited {
			color: #c6c6c6;
		}
	}
}
```
