// JumpRunShoot - Canvas & Input Interop
window.gameInterop = {
    canvas: null,
    ctx: null,
    imageData: null,
    dotNetHelper: null,
    animFrameId: null,
    isRunning: false,
    currentTheme: "duke",

    getThemeTrack: function (themeName) {
        const name = (themeName || this.currentTheme || "").toLowerCase();
        if (name.endsWith(".wav")) {
            return name;
        }
        if (name.includes("underwater") || name.includes("ocean")) {
            return "audio/bgm_underwater.wav";
        }
        if (name.includes("forest") || name.includes("hunter")) {
            return "audio/bgm_forest.wav";
        }
        if (name.includes("space") || name.includes("invader")) {
            return "audio/bgm_space.wav";
        }
        return "audio/bgm_duke.wav";
    },

    setTheme: function (themeName) {
        this.currentTheme = (themeName || "").toLowerCase();
        if (this.bgm) {
            this.bgm.onThemeChanged(this.currentTheme);
        }
    },

    preventKeys: new Set([
        "ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight",
        "Space", "ControlLeft", "ControlRight", "AltLeft", "AltRight",
        "KeyZ", "KeyX", "KeyJ", "KeyK", "KeyW", "KeyA", "KeyS", "KeyD"
    ]),

    init: function (canvasId, dotNetHelper) {
        this.canvas = document.getElementById(canvasId);
        if (!this.canvas) {
            console.error("Canvas element not found:", canvasId);
            return false;
        }

        this.ctx = this.canvas.getContext("2d", { alpha: false, desynchronized: true });
        this.ctx.imageSmoothingEnabled = false;
        this.ctx.webkitImageSmoothingEnabled = false;
        this.ctx.mozImageSmoothingEnabled = false;
        this.ctx.msImageSmoothingEnabled = false;
        this.imageData = this.ctx.createImageData(320, 200);
        this.dotNetHelper = dotNetHelper;

        this.setupKeyboard();
        this.setupAudioTrigger();
        return true;
    },

    setupAudioTrigger: function () {
        const self = this;
        const unlockAudio = function () {
            self.audio.init();
            self.audio.resume();
            const sfxEnabled = self.getSfxEnabled();
            self.audio.setMuted(!sfxEnabled);
            window.removeEventListener("keydown", unlockAudio);
            window.removeEventListener("pointerdown", unlockAudio);
        };
        window.addEventListener("keydown", unlockAudio, { once: true, passive: true });
        window.addEventListener("pointerdown", unlockAudio, { once: true, passive: true });
    },

    setupKeyboard: function () {
        const self = this;

        const isFormField = function (el) {
            if (!el || !el.tagName) return false;
            const tag = el.tagName.toUpperCase();
            return tag === "INPUT" || tag === "SELECT" || tag === "TEXTAREA";
        };

        window.addEventListener("focusin", function (e) {
            if (isFormField(e.target) && self.dotNetHelper) {
                self.dotNetHelper.invokeMethodAsync("OnBlur");
            }
        });

        window.addEventListener("keydown", function (e) {
            self.audio.resume();
            if (isFormField(e.target) || isFormField(document.activeElement)) {
                return;
            }
            if (self.preventKeys.has(e.code) || (e.altKey && !e.ctrlKey)) {
                e.preventDefault();
            }
            if (self.dotNetHelper) {
                self.dotNetHelper.invokeMethodAsync("OnKeyDown", e.code);
            }
        }, { passive: false });

        window.addEventListener("keyup", function (e) {
            if (isFormField(e.target) || isFormField(document.activeElement)) {
                return;
            }
            if (self.preventKeys.has(e.code)) {
                e.preventDefault();
            }
            if (self.dotNetHelper) {
                self.dotNetHelper.invokeMethodAsync("OnKeyUp", e.code);
            }
        }, { passive: false });

        window.addEventListener("blur", function () {
            if (self.dotNetHelper) {
                self.dotNetHelper.invokeMethodAsync("OnBlur");
            }
        });
    },

    startLoop: function () {
        if (this.isRunning) return;
        this.isRunning = true;
        const self = this;

        function loop(timestamp) {
            if (!self.isRunning) return;
            if (self.dotNetHelper) {
                self.dotNetHelper.invokeMethodAsync("OnAnimationFrame", timestamp);
            }
            self.animFrameId = requestAnimationFrame(loop);
        }

        this.animFrameId = requestAnimationFrame(loop);
    },

    stopLoop: function () {
        this.isRunning = false;
        if (this.animFrameId) {
            cancelAnimationFrame(this.animFrameId);
            this.animFrameId = null;
        }
    },

    renderFrame: function (pixelBytes, audioFlags) {
        if (this.ctx && this.imageData) {
            this.imageData.data.set(pixelBytes);
            this.ctx.putImageData(this.imageData, 0, 0);
        }
        if (audioFlags) {
            this.audio.dispatchFlags(audioFlags);
        }
    },

    focusCanvas: function () {
        if (this.canvas) {
            this.canvas.focus();
        }
    },

    getPlayerName: function () {
        return localStorage.getItem("jrs_player_name") || "Unknown";
    },

    setPlayerName: function (name) {
        localStorage.setItem("jrs_player_name", name || "Unknown");
    },

    getLastSeed: function () {
        return localStorage.getItem("jrs_last_seed") || "";
    },

    setLastSeed: function (seed) {
        if (seed && typeof seed === "string") {
            const clean = seed.trim();
            if (clean.length > 0) {
                localStorage.setItem("jrs_last_seed", clean);
            }
        }
    },

    getSfxEnabled: function () {
        const val = localStorage.getItem("jrs_sfx_enabled");
        if (val === null) return true;
        return val === "true";
    },

    setSfxEnabled: function (enabled) {
        localStorage.setItem("jrs_sfx_enabled", enabled ? "true" : "false");
        this.audio.setMuted(!enabled);
    },

    getHighScores: function () {
        try {
            const raw = localStorage.getItem("jrs_high_scores");
            return raw ? JSON.parse(raw) : [];
        } catch {
            return [];
        }
    },

    saveHighScore: function (name, score, seed) {
        if (!score || score <= 0) return { rank: 0, isNewHigh: false, scores: this.getHighScores() };
        try {
            let scores = this.getHighScores();
            const dateStr = new Date().toISOString().slice(0, 10);
            const entry = {
                name: (name || "Unknown").trim().toUpperCase().slice(0, 12),
                score: parseInt(score, 10),
                seed: (seed || "FOO").trim().toUpperCase(),
                date: dateStr
            };
            scores.push(entry);
            scores.sort((a, b) => b.score - a.score);
            const rankIndex = scores.indexOf(entry);
            scores = scores.slice(0, 10);
            localStorage.setItem("jrs_high_scores", JSON.stringify(scores));
            if (seed) {
                this.setLastSeed(seed);
            }
            return {
                rank: rankIndex + 1,
                isNewHigh: rankIndex === 0,
                scores: scores
            };
        } catch (e) {
            console.error("Failed to save high score:", e);
            return { rank: 0, isNewHigh: false, scores: [] };
        }
    },

    clearHighScores: function () {
        localStorage.removeItem("jrs_high_scores");
        return [];
    },

    bgm: {
        audio: null,
        isPlaying: false,
        isMuted: true,
        currentTrack: "",
        init: function () {
            if (!this.audio) {
                const track = window.gameInterop.getThemeTrack();
                this.audio = new Audio(track);
                this.audio.loop = true;
                this.audio.volume = 0.35;
                this.currentTrack = track;
            }
        },
        onThemeChanged: function (themeName) {
            const nextTrack = window.gameInterop.getThemeTrack(themeName);
            if (this.currentTrack === nextTrack) return;
            this.currentTrack = nextTrack;
            if (this.audio) {
                const wasPlaying = this.isPlaying && !this.isMuted;
                this.audio.pause();
                this.audio.src = nextTrack;
                this.audio.load();
                if (wasPlaying) {
                    this.audio.play().catch(() => {});
                }
            }
        },
        toggle: function () {
            this.init();
            this.isMuted = !this.isMuted;
            if (!this.isMuted) {
                this.audio.play().catch(() => {});
                this.isPlaying = true;
            } else {
                this.audio.pause();
                this.isPlaying = false;
            }
            return !this.isMuted;
        },
        setMuted: function (muted) {
            this.init();
            this.isMuted = muted;
            if (!this.isMuted) {
                this.audio.play().catch(() => {});
                this.isPlaying = true;
            } else {
                this.audio.pause();
                this.isPlaying = false;
            }
            return !this.isMuted;
        },
        pause: function () {
            if (this.audio) this.audio.pause();
        },
        resume: function () {
            if (this.audio && !this.isMuted && this.isPlaying) {
                this.audio.play().catch(() => {});
            }
        }
    },

    // Web Audio API Retro Sound Effects Synthesizer (Zero asset download latency)
    audio: {
        ctx: null,
        masterGain: null,
        noiseBuffer: null,
        isMuted: false,

        init: function () {
            if (this.ctx) return;
            const AudioCtx = window.AudioContext || window.webkitAudioContext;
            if (!AudioCtx) return;

            this.ctx = new AudioCtx();
            this.masterGain = this.ctx.createGain();
            const sfxEnabled = window.gameInterop ? window.gameInterop.getSfxEnabled() : true;
            this.isMuted = !sfxEnabled;
            this.masterGain.gain.setValueAtTime(this.isMuted ? 0 : 0.35, this.ctx.currentTime);
            this.masterGain.connect(this.ctx.destination);

            // Pre-generate 1-second white noise buffer for crunchy explosions
            const bufferSize = this.ctx.sampleRate;
            this.noiseBuffer = this.ctx.createBuffer(1, bufferSize, this.ctx.sampleRate);
            const data = this.noiseBuffer.getChannelData(0);
            for (let i = 0; i < bufferSize; i++) {
                data[i] = Math.random() * 2 - 1;
            }
        },

        toggle: function () {
            this.init();
            this.isMuted = !this.isMuted;
            if (this.masterGain && this.ctx) {
                this.masterGain.gain.setValueAtTime(this.isMuted ? 0 : 0.35, this.ctx.currentTime);
            }
            try {
                localStorage.setItem("jrs_sfx_enabled", (!this.isMuted).toString());
            } catch (e) {}
            return !this.isMuted;
        },

        setMuted: function (muted) {
            this.init();
            this.isMuted = muted;
            if (this.masterGain && this.ctx) {
                this.masterGain.gain.setValueAtTime(muted ? 0 : 0.35, this.ctx.currentTime);
            }
            try {
                localStorage.setItem("jrs_sfx_enabled", (!muted).toString());
            } catch (e) {}
            return !this.isMuted;
        },

        resume: function () {
            if (this.ctx && this.ctx.state === "suspended") {
                this.ctx.resume();
            }
        },

        // Player Blaster Shoot: snappy high-to-mid retro chirp (880Hz -> 220Hz over 0.08s)
        playPlayerShoot: function () {
            if (!this.ctx) return;
            this.resume();
            const now = this.ctx.currentTime;

            if (window.gameInterop && (window.gameInterop.currentTheme.includes("underwater") || window.gameInterop.currentTheme.includes("ocean"))) {
                // Aquatic Speargun Bubble Harpoon: deep pneumatic pop + high bubble chirps
                const osc = this.ctx.createOscillator();
                const gain = this.ctx.createGain();
                osc.type = "sine";
                osc.frequency.setValueAtTime(560, now);
                osc.frequency.exponentialRampToValueAtTime(140, now + 0.11);
                gain.gain.setValueAtTime(0.28, now);
                gain.gain.exponentialRampToValueAtTime(0.001, now + 0.11);
                osc.connect(gain);
                gain.connect(this.masterGain);
                osc.start(now);
                osc.stop(now + 0.11);
                return;
            }

            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();

            osc.type = "square";
            osc.frequency.setValueAtTime(880, now);
            osc.frequency.exponentialRampToValueAtTime(220, now + 0.08);

            gain.gain.setValueAtTime(0.25, now);
            gain.gain.exponentialRampToValueAtTime(0.001, now + 0.08);

            osc.connect(gain);
            gain.connect(this.masterGain);

            osc.start(now);
            osc.stop(now + 0.08);
        },

        // Player Footstep: crisp mechanical floor tap
        // rateMultiplier: 1.0 for walking, 1.4 for running (accelerated cadence & higher pitch)
        playStep: function (rateMultiplier) {
            if (!this.ctx) return;
            this.resume();
            const now = this.ctx.currentTime;
            const mult = rateMultiplier || 1.0;

            if (window.gameInterop && (window.gameInterop.currentTheme.includes("underwater") || window.gameInterop.currentTheme.includes("ocean"))) {
                // Gentle submerged water flutter / bubble blip
                const osc = this.ctx.createOscillator();
                const gain = this.ctx.createGain();
                osc.type = "sine";
                const baseFreq = 300 * mult;
                osc.frequency.setValueAtTime(baseFreq, now);
                osc.frequency.exponentialRampToValueAtTime(baseFreq * 0.7, now + 0.04);
                gain.gain.setValueAtTime(0.08, now);
                gain.gain.exponentialRampToValueAtTime(0.001, now + 0.04);
                osc.connect(gain);
                gain.connect(this.masterGain);
                osc.start(now);
                osc.stop(now + 0.04);
                return;
            }

            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();

            osc.type = "triangle";
            const baseFreq = 160 * mult;
            const endFreq = 60 * mult;
            const dur = 0.04 / mult;

            osc.frequency.setValueAtTime(baseFreq, now);
            osc.frequency.exponentialRampToValueAtTime(endFreq, now + dur);

            gain.gain.setValueAtTime(0.15, now);
            gain.gain.exponentialRampToValueAtTime(0.001, now + dur);

            osc.connect(gain);
            gain.connect(this.masterGain);

            osc.start(now);
            osc.stop(now + dur);
        },

        // Player Jump: springy ascending retro tone sweep (160Hz -> 480Hz over 0.12s)
        playPlayerJump: function () {
            if (!this.ctx) return;
            this.resume();
            const now = this.ctx.currentTime;

            if (window.gameInterop && (window.gameInterop.currentTheme.includes("underwater") || window.gameInterop.currentTheme.includes("ocean"))) {
                // Hydro-jet flipper kick / water swoosh
                const osc = this.ctx.createOscillator();
                const gain = this.ctx.createGain();
                osc.type = "triangle";
                osc.frequency.setValueAtTime(180, now);
                osc.frequency.exponentialRampToValueAtTime(320, now + 0.14);
                gain.gain.setValueAtTime(0.20, now);
                gain.gain.exponentialRampToValueAtTime(0.001, now + 0.14);
                osc.connect(gain);
                gain.connect(this.masterGain);
                osc.start(now);
                osc.stop(now + 0.14);
                return;
            }

            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();

            osc.type = "square";
            osc.frequency.setValueAtTime(160, now);
            osc.frequency.exponentialRampToValueAtTime(480, now + 0.12);

            gain.gain.setValueAtTime(0.22, now);
            gain.gain.exponentialRampToValueAtTime(0.001, now + 0.12);

            osc.connect(gain);
            gain.connect(this.masterGain);

            osc.start(now);
            osc.stop(now + 0.12);
        },

        // Monster Shoot: robotic descending sawtooth zap (420Hz -> 140Hz over 0.10s)
        playMonsterShoot: function () {
            if (!this.ctx) return;
            this.resume();
            const now = this.ctx.currentTime;

            if (window.gameInterop && (window.gameInterop.currentTheme.includes("underwater") || window.gameInterop.currentTheme.includes("ocean"))) {
                // Bio-electric aquatic pulse
                const osc = this.ctx.createOscillator();
                const gain = this.ctx.createGain();
                osc.type = "sine";
                osc.frequency.setValueAtTime(360, now);
                osc.frequency.exponentialRampToValueAtTime(110, now + 0.12);
                gain.gain.setValueAtTime(0.22, now);
                gain.gain.exponentialRampToValueAtTime(0.001, now + 0.12);
                osc.connect(gain);
                gain.connect(this.masterGain);
                osc.start(now);
                osc.stop(now + 0.12);
                return;
            }

            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();

            osc.type = "sawtooth";
            osc.frequency.setValueAtTime(420, now);
            osc.frequency.exponentialRampToValueAtTime(140, now + 0.10);

            gain.gain.setValueAtTime(0.20, now);
            gain.gain.exponentialRampToValueAtTime(0.001, now + 0.10);

            osc.connect(gain);
            gain.connect(this.masterGain);

            osc.start(now);
            osc.stop(now + 0.10);
        },

        // Monster Die: crunchy retro explosion (filtered noise burst + descending pitch thump)
        playMonsterDie: function () {
            if (!this.ctx) return;
            this.resume();
            const now = this.ctx.currentTime;

            if (window.gameInterop && (window.gameInterop.currentTheme.includes("underwater") || window.gameInterop.currentTheme.includes("ocean"))) {
                // Muffled depth charge rumble / underwater bubble burst
                const osc = this.ctx.createOscillator();
                const oscGain = this.ctx.createGain();
                osc.type = "triangle";
                osc.frequency.setValueAtTime(130, now);
                osc.frequency.exponentialRampToValueAtTime(30, now + 0.22);
                oscGain.gain.setValueAtTime(0.30, now);
                oscGain.gain.exponentialRampToValueAtTime(0.001, now + 0.22);
                osc.connect(oscGain);
                oscGain.connect(this.masterGain);
                osc.start(now);
                osc.stop(now + 0.22);
                return;
            }

            // 1. Filtered noise burst
            if (this.noiseBuffer) {
                const noise = this.ctx.createBufferSource();
                noise.buffer = this.noiseBuffer;
                const filter = this.ctx.createBiquadFilter();
                filter.type = "lowpass";
                filter.frequency.setValueAtTime(1200, now);
                filter.frequency.exponentialRampToValueAtTime(200, now + 0.18);

                const noiseGain = this.ctx.createGain();
                noiseGain.gain.setValueAtTime(0.35, now);
                noiseGain.gain.exponentialRampToValueAtTime(0.001, now + 0.18);

                noise.connect(filter);
                filter.connect(noiseGain);
                noiseGain.connect(this.masterGain);

                noise.start(now);
                noise.stop(now + 0.18);
            }

            // 2. Low-frequency punch
            const osc = this.ctx.createOscillator();
            const oscGain = this.ctx.createGain();
            osc.type = "square";
            osc.frequency.setValueAtTime(180, now);
            osc.frequency.exponentialRampToValueAtTime(35, now + 0.16);

            oscGain.gain.setValueAtTime(0.25, now);
            oscGain.gain.exponentialRampToValueAtTime(0.001, now + 0.16);

            osc.connect(oscGain);
            oscGain.connect(this.masterGain);

            osc.start(now);
            osc.stop(now + 0.16);
        },

        // Dedicated theme- and monster-specific death sound synthesis (5 monster types x 3 themes)
        playMonsterTypeDie: function (type) {
            if (!this.ctx) return;
            this.resume();
            const now = this.ctx.currentTime;
            const theme = (window.gameInterop && window.gameInterop.currentTheme) || "";

            if (theme.includes("space") || theme.includes("invader")) {
                this.playSpaceMonsterDie(type, now);
            } else if (theme.includes("underwater") || theme.includes("ocean")) {
                this.playUnderwaterMonsterDie(type, now);
            } else if (theme.includes("forest") || theme.includes("hunter")) {
                this.playForestMonsterDie(type, now);
            } else {
                this.playDukeMonsterDie(type, now);
            }
        },

        playSpaceMonsterDie: function (type, now) {
            switch (type) {
                case 0: { // Alien Scout Saucer / Crab: Classic 8-bit descending arcade pop & zap
                    const osc = this.ctx.createOscillator();
                    const gain = this.ctx.createGain();
                    osc.type = "square";
                    osc.frequency.setValueAtTime(960, now);
                    osc.frequency.exponentialRampToValueAtTime(110, now + 0.10);
                    gain.gain.setValueAtTime(0.24, now);
                    gain.gain.exponentialRampToValueAtTime(0.001, now + 0.10);
                    osc.connect(gain);
                    gain.connect(this.masterGain);
                    osc.start(now);
                    osc.stop(now + 0.10);

                    const noise = this.createNoiseBuffer(0.06);
                    if (noise) {
                        const filter = this.ctx.createBiquadFilter();
                        filter.type = "bandpass";
                        filter.frequency.setValueAtTime(1600, now);
                        filter.Q.value = 2.0;
                        const nGain = this.ctx.createGain();
                        nGain.gain.setValueAtTime(0.18, now);
                        nGain.gain.exponentialRampToValueAtTime(0.001, now + 0.06);
                        noise.connect(filter);
                        filter.connect(nGain);
                        nGain.connect(this.masterGain);
                        noise.start(now);
                        noise.stop(now + 0.06);
                    }
                    break;
                }
                case 1: { // Flying Saucer UFO: Extraterrestrial modulated vibrato explosion
                    const osc = this.ctx.createOscillator();
                    const mod = this.ctx.createOscillator();
                    const modGain = this.ctx.createGain();
                    const gain = this.ctx.createGain();

                    mod.type = "sawtooth";
                    mod.frequency.setValueAtTime(45, now);
                    modGain.gain.setValueAtTime(220, now);
                    modGain.gain.exponentialRampToValueAtTime(10, now + 0.18);
                    mod.connect(modGain);
                    modGain.connect(osc.frequency);

                    osc.type = "triangle";
                    osc.frequency.setValueAtTime(740, now);
                    osc.frequency.exponentialRampToValueAtTime(90, now + 0.18);
                    gain.gain.setValueAtTime(0.25, now);
                    gain.gain.exponentialRampToValueAtTime(0.001, now + 0.18);

                    osc.connect(gain);
                    gain.connect(this.masterGain);
                    mod.start(now);
                    osc.start(now);
                    mod.stop(now + 0.18);
                    osc.stop(now + 0.18);
                    break;
                }
                case 2: { // Orbital Defense Satellite: High-voltage capacitor discharge & laser crackle
                    const osc = this.ctx.createOscillator();
                    const gain = this.ctx.createGain();
                    osc.type = "sawtooth";
                    osc.frequency.setValueAtTime(1450, now);
                    osc.frequency.exponentialRampToValueAtTime(160, now + 0.14);
                    gain.gain.setValueAtTime(0.22, now);
                    gain.gain.exponentialRampToValueAtTime(0.001, now + 0.14);
                    osc.connect(gain);
                    gain.connect(this.masterGain);
                    osc.start(now);
                    osc.stop(now + 0.14);

                    const noise = this.createNoiseBuffer(0.14);
                    if (noise) {
                        const filter = this.ctx.createBiquadFilter();
                        filter.type = "highpass";
                        filter.frequency.setValueAtTime(2200, now);
                        const nGain = this.ctx.createGain();
                        nGain.gain.setValueAtTime(0.19, now);
                        nGain.gain.exponentialRampToValueAtTime(0.001, now + 0.14);
                        noise.connect(filter);
                        filter.connect(nGain);
                        nGain.connect(this.masterGain);
                        noise.start(now);
                        noise.stop(now + 0.14);
                    }
                    break;
                }
                case 3: { // Fast Attack Interceptor: Supersonic Doppler crash & thruster sputter
                    const osc = this.ctx.createOscillator();
                    const gain = this.ctx.createGain();
                    osc.type = "sawtooth";
                    osc.frequency.setValueAtTime(1050, now);
                    osc.frequency.exponentialRampToValueAtTime(75, now + 0.22);
                    gain.gain.setValueAtTime(0.28, now);
                    gain.gain.exponentialRampToValueAtTime(0.001, now + 0.22);
                    osc.connect(gain);
                    gain.connect(this.masterGain);
                    osc.start(now);
                    osc.stop(now + 0.22);

                    const noise = this.createNoiseBuffer(0.22);
                    if (noise) {
                        const filter = this.ctx.createBiquadFilter();
                        filter.type = "lowpass";
                        filter.frequency.setValueAtTime(1200, now);
                        const nGain = this.ctx.createGain();
                        nGain.gain.setValueAtTime(0.22, now);
                        nGain.gain.exponentialRampToValueAtTime(0.001, now + 0.22);
                        noise.connect(filter);
                        filter.connect(nGain);
                        nGain.connect(this.masterGain);
                        noise.start(now);
                        noise.stop(now + 0.22);
                    }
                    break;
                }
                case 4: { // Alien Mothership Dreadnought: Deep-space reactor core meltdown
                    const sub = this.ctx.createOscillator();
                    const subGain = this.ctx.createGain();
                    sub.type = "sawtooth";
                    sub.frequency.setValueAtTime(120, now);
                    sub.frequency.exponentialRampToValueAtTime(32, now + 0.45);
                    subGain.gain.setValueAtTime(0.36, now);
                    subGain.gain.exponentialRampToValueAtTime(0.001, now + 0.45);
                    sub.connect(subGain);
                    subGain.connect(this.masterGain);
                    sub.start(now);
                    sub.stop(now + 0.45);

                    const noise = this.createNoiseBuffer(0.45);
                    if (noise) {
                        const filter = this.ctx.createBiquadFilter();
                        filter.type = "lowpass";
                        filter.frequency.setValueAtTime(650, now);
                        filter.frequency.exponentialRampToValueAtTime(100, now + 0.45);
                        const nGain = this.ctx.createGain();
                        nGain.gain.setValueAtTime(0.32, now);
                        nGain.gain.exponentialRampToValueAtTime(0.001, now + 0.45);
                        noise.connect(filter);
                        filter.connect(nGain);
                        nGain.connect(this.masterGain);
                        noise.start(now);
                        noise.stop(now + 0.45);
                    }
                    break;
                }
            }
        },

        // Duke 1 Classic Theme: Cyber-industrial robotic explosions and servo meltdowns
        playDukeMonsterDie: function (type, now) {
            switch (type) {
                case 0: { // PatrolDroid: Short-circuit capacitor pop & electrical crackle
                    const osc = this.ctx.createOscillator();
                    const gain = this.ctx.createGain();
                    osc.type = "square";
                    osc.frequency.setValueAtTime(760, now);
                    osc.frequency.exponentialRampToValueAtTime(120, now + 0.08);
                    gain.gain.setValueAtTime(0.22, now);
                    gain.gain.exponentialRampToValueAtTime(0.001, now + 0.08);
                    osc.connect(gain);
                    gain.connect(this.masterGain);
                    osc.start(now);
                    osc.stop(now + 0.08);

                    if (this.noiseBuffer) {
                        const noise = this.ctx.createBufferSource();
                        noise.buffer = this.noiseBuffer;
                        const filter = this.ctx.createBiquadFilter();
                        filter.type = "bandpass";
                        filter.frequency.setValueAtTime(1600, now);
                        filter.Q.setValueAtTime(2.5, now);
                        const nGain = this.ctx.createGain();
                        nGain.gain.setValueAtTime(0.26, now);
                        nGain.gain.exponentialRampToValueAtTime(0.001, now + 0.12);
                        noise.connect(filter);
                        filter.connect(nGain);
                        nGain.connect(this.masterGain);
                        noise.start(now);
                        noise.stop(now + 0.12);
                    }
                    break;
                }
                case 1: { // HoverDrone: Gyro-turbine blowout & descending pitch dive
                    const osc = this.ctx.createOscillator();
                    const gain = this.ctx.createGain();
                    osc.type = "sawtooth";
                    osc.frequency.setValueAtTime(1200, now);
                    osc.frequency.exponentialRampToValueAtTime(80, now + 0.18);
                    gain.gain.setValueAtTime(0.24, now);
                    gain.gain.exponentialRampToValueAtTime(0.001, now + 0.18);

                    // 45Hz rotor flutter modulation
                    const am = this.ctx.createOscillator();
                    const amGain = this.ctx.createGain();
                    am.type = "square";
                    am.frequency.setValueAtTime(45, now);
                    amGain.gain.setValueAtTime(0.08, now);
                    am.connect(amGain);
                    amGain.connect(gain.gain);
                    am.start(now);
                    am.stop(now + 0.18);

                    osc.connect(gain);
                    gain.connect(this.masterGain);
                    osc.start(now);
                    osc.stop(now + 0.18);
                    break;
                }
                case 2: { // CeilingTurret: High-voltage capacitor overload zap & metallic chassis clatter
                    const osc = this.ctx.createOscillator();
                    const gain = this.ctx.createGain();
                    osc.type = "triangle";
                    osc.frequency.setValueAtTime(1800, now);
                    osc.frequency.exponentialRampToValueAtTime(260, now + 0.06);
                    gain.gain.setValueAtTime(0.25, now);
                    gain.gain.exponentialRampToValueAtTime(0.001, now + 0.06);
                    osc.connect(gain);
                    gain.connect(this.masterGain);
                    osc.start(now);
                    osc.stop(now + 0.06);

                    // Metallic clang
                    const clang = this.ctx.createOscillator();
                    const cGain = this.ctx.createGain();
                    clang.type = "sawtooth";
                    clang.frequency.setValueAtTime(520, now);
                    clang.frequency.exponentialRampToValueAtTime(110, now + 0.22);
                    cGain.gain.setValueAtTime(0.22, now);
                    cGain.gain.exponentialRampToValueAtTime(0.001, now + 0.22);
                    clang.connect(cGain);
                    cGain.connect(this.masterGain);
                    clang.start(now);
                    clang.stop(now + 0.22);
                    break;
                }
                case 3: { // MechaHound: Dual-detuned servo screech & hydraulic steam release
                    const osc1 = this.ctx.createOscillator();
                    const osc2 = this.ctx.createOscillator();
                    const gain = this.ctx.createGain();
                    osc1.type = "sawtooth";
                    osc2.type = "sawtooth";
                    osc1.frequency.setValueAtTime(820, now);
                    osc2.frequency.setValueAtTime(855, now);
                    osc1.frequency.exponentialRampToValueAtTime(280, now + 0.10);
                    osc2.frequency.exponentialRampToValueAtTime(295, now + 0.10);
                    gain.gain.setValueAtTime(0.20, now);
                    gain.gain.exponentialRampToValueAtTime(0.001, now + 0.10);
                    osc1.connect(gain);
                    osc2.connect(gain);
                    gain.connect(this.masterGain);
                    osc1.start(now);
                    osc2.start(now);
                    osc1.stop(now + 0.10);
                    osc2.stop(now + 0.10);

                    if (this.noiseBuffer) {
                        const hiss = this.ctx.createBufferSource();
                        hiss.buffer = this.noiseBuffer;
                        const filter = this.ctx.createBiquadFilter();
                        filter.type = "highpass";
                        filter.frequency.setValueAtTime(2200, now);
                        const hGain = this.ctx.createGain();
                        hGain.gain.setValueAtTime(0.25, now);
                        hGain.gain.exponentialRampToValueAtTime(0.001, now + 0.24);
                        hiss.connect(filter);
                        filter.connect(hGain);
                        hGain.connect(this.masterGain);
                        hiss.start(now);
                        hiss.stop(now + 0.24);
                    }
                    break;
                }
                case 4: // HeavyGuard: Massive thunderous dual-stage heavy mech explosion
                default: {
                    const sub = this.ctx.createOscillator();
                    const subGain = this.ctx.createGain();
                    sub.type = "sine";
                    sub.frequency.setValueAtTime(190, now);
                    sub.frequency.exponentialRampToValueAtTime(28, now + 0.36);
                    subGain.gain.setValueAtTime(0.38, now);
                    subGain.gain.exponentialRampToValueAtTime(0.001, now + 0.36);
                    sub.connect(subGain);
                    subGain.connect(this.masterGain);
                    sub.start(now);
                    sub.stop(now + 0.36);

                    if (this.noiseBuffer) {
                        const blast = this.ctx.createBufferSource();
                        blast.buffer = this.noiseBuffer;
                        const filter = this.ctx.createBiquadFilter();
                        filter.type = "lowpass";
                        filter.frequency.setValueAtTime(900, now);
                        filter.frequency.exponentialRampToValueAtTime(120, now + 0.48);
                        const bGain = this.ctx.createGain();
                        bGain.gain.setValueAtTime(0.40, now);
                        bGain.gain.exponentialRampToValueAtTime(0.001, now + 0.48);
                        blast.connect(filter);
                        filter.connect(bGain);
                        bGain.connect(this.masterGain);
                        blast.start(now);
                        blast.stop(now + 0.48);
                    }
                    break;
                }
            }
        },

        // Forest & Creatures Theme: Organic woodland beast shrieks, squeals, and rustles
        playForestMonsterDie: function (type, now) {
            switch (type) {
                case 0: { // Spiky Boar: Deep guttural grunt / squeal thump with foliage impact
                    const grunt = this.ctx.createOscillator();
                    const gGain = this.ctx.createGain();
                    grunt.type = "triangle";
                    grunt.frequency.setValueAtTime(210, now);
                    grunt.frequency.exponentialRampToValueAtTime(50, now + 0.18);
                    gGain.gain.setValueAtTime(0.28, now);
                    gGain.gain.exponentialRampToValueAtTime(0.001, now + 0.18);
                    grunt.connect(gGain);
                    gGain.connect(this.masterGain);
                    grunt.start(now);
                    grunt.stop(now + 0.18);

                    if (this.noiseBuffer) {
                        const thud = this.ctx.createBufferSource();
                        thud.buffer = this.noiseBuffer;
                        const filter = this.ctx.createBiquadFilter();
                        filter.type = "lowpass";
                        filter.frequency.setValueAtTime(240, now);
                        const tGain = this.ctx.createGain();
                        tGain.gain.setValueAtTime(0.32, now);
                        tGain.gain.exponentialRampToValueAtTime(0.001, now + 0.15);
                        thud.connect(filter);
                        filter.connect(tGain);
                        tGain.connect(this.masterGain);
                        thud.start(now);
                        thud.stop(now + 0.15);
                    }
                    break;
                }
                case 1: { // Giant Hornet: Chitinous wing flutter snap & piercing insect screech
                    const buzz = this.ctx.createOscillator();
                    const bGain = this.ctx.createGain();
                    buzz.type = "sawtooth";
                    buzz.frequency.setValueAtTime(980, now);
                    buzz.frequency.exponentialRampToValueAtTime(240, now + 0.14);
                    bGain.gain.setValueAtTime(0.24, now);
                    bGain.gain.exponentialRampToValueAtTime(0.001, now + 0.14);

                    const fm = this.ctx.createOscillator();
                    const fmGain = this.ctx.createGain();
                    fm.type = "square";
                    fm.frequency.setValueAtTime(220, now);
                    fmGain.gain.setValueAtTime(200, now);
                    fm.connect(fmGain);
                    fmGain.connect(buzz.frequency);
                    fm.start(now);
                    fm.stop(now + 0.14);

                    buzz.connect(bGain);
                    bGain.connect(this.masterGain);
                    buzz.start(now);
                    buzz.stop(now + 0.14);
                    break;
                }
                case 2: { // Canopy Spider: Venomous web-sac rupture & acidic squelch
                    if (this.noiseBuffer) {
                        const squelch = this.ctx.createBufferSource();
                        squelch.buffer = this.noiseBuffer;
                        const filter = this.ctx.createBiquadFilter();
                        filter.type = "bandpass";
                        filter.frequency.setValueAtTime(1800, now);
                        filter.frequency.exponentialRampToValueAtTime(280, now + 0.20);
                        filter.Q.setValueAtTime(4.0, now);
                        const sGain = this.ctx.createGain();
                        sGain.gain.setValueAtTime(0.28, now);
                        sGain.gain.exponentialRampToValueAtTime(0.001, now + 0.20);
                        squelch.connect(filter);
                        filter.connect(sGain);
                        sGain.connect(this.masterGain);
                        squelch.start(now);
                        squelch.stop(now + 0.20);
                    }

                    const drop = this.ctx.createOscillator();
                    const dGain = this.ctx.createGain();
                    drop.type = "triangle";
                    drop.frequency.setValueAtTime(340, now);
                    drop.frequency.exponentialRampToValueAtTime(90, now + 0.12);
                    dGain.gain.setValueAtTime(0.18, now);
                    dGain.gain.exponentialRampToValueAtTime(0.001, now + 0.12);
                    drop.connect(dGain);
                    dGain.connect(this.masterGain);
                    drop.start(now);
                    drop.stop(now + 0.12);
                    break;
                }
                case 3: { // Timber Wolf: Wild woodland wolf death yelp & fading howl
                    const yelp = this.ctx.createOscillator();
                    const yGain = this.ctx.createGain();
                    yelp.type = "sine";
                    yelp.frequency.setValueAtTime(560, now);
                    yelp.frequency.exponentialRampToValueAtTime(180, now + 0.26);
                    yGain.gain.setValueAtTime(0.26, now);
                    yGain.gain.exponentialRampToValueAtTime(0.001, now + 0.26);

                    // 8Hz wolf vibrato
                    const vib = this.ctx.createOscillator();
                    const vGain = this.ctx.createGain();
                    vib.frequency.setValueAtTime(8, now);
                    vGain.gain.setValueAtTime(15, now);
                    vib.connect(vGain);
                    vGain.connect(yelp.frequency);
                    vib.start(now);
                    vib.stop(now + 0.26);

                    yelp.connect(yGain);
                    yGain.connect(this.masterGain);
                    yelp.start(now);
                    yelp.stop(now + 0.26);
                    break;
                }
                case 4: // Grizzly Bear: Deep primal bear roar / bellow
                default: {
                    const roar = this.ctx.createOscillator();
                    const rGain = this.ctx.createGain();
                    roar.type = "sawtooth";
                    roar.frequency.setValueAtTime(105, now);
                    roar.frequency.exponentialRampToValueAtTime(32, now + 0.42);
                    rGain.gain.setValueAtTime(0.36, now);
                    rGain.gain.exponentialRampToValueAtTime(0.001, now + 0.42);

                    const fm = this.ctx.createOscillator();
                    const fmGain = this.ctx.createGain();
                    fm.type = "triangle";
                    fm.frequency.setValueAtTime(42, now);
                    fmGain.gain.setValueAtTime(60, now);
                    fm.connect(fmGain);
                    fmGain.connect(roar.frequency);
                    fm.start(now);
                    fm.stop(now + 0.42);

                    roar.connect(rGain);
                    rGain.connect(this.masterGain);
                    roar.start(now);
                    roar.stop(now + 0.42);
                    break;
                }
            }
        },

        // Underwater Theme: Hydro-acoustic pops, water chimes, and deep depth-charge blasts
        playUnderwaterMonsterDie: function (type, now) {
            switch (type) {
                case 0: { // Sea Urchin / Crab: Chitinous shell crack & aquatic muffled pop
                    if (this.noiseBuffer) {
                        const pop = this.ctx.createBufferSource();
                        pop.buffer = this.noiseBuffer;
                        const filter = this.ctx.createBiquadFilter();
                        filter.type = "bandpass";
                        filter.frequency.setValueAtTime(480, now);
                        filter.Q.setValueAtTime(3.0, now);
                        const pGain = this.ctx.createGain();
                        pGain.gain.setValueAtTime(0.28, now);
                        pGain.gain.exponentialRampToValueAtTime(0.001, now + 0.08);
                        pop.connect(filter);
                        filter.connect(pGain);
                        pGain.connect(this.masterGain);
                        pop.start(now);
                        pop.stop(now + 0.08);
                    }

                    const ping = this.ctx.createOscillator();
                    const pingGain = this.ctx.createGain();
                    ping.type = "sine";
                    ping.frequency.setValueAtTime(760, now);
                    ping.frequency.exponentialRampToValueAtTime(380, now + 0.10);
                    pingGain.gain.setValueAtTime(0.22, now);
                    pingGain.gain.exponentialRampToValueAtTime(0.001, now + 0.10);
                    ping.connect(pingGain);
                    pingGain.connect(this.masterGain);
                    ping.start(now);
                    ping.stop(now + 0.10);
                    break;
                }
                case 1: { // Bioluminescent Jellyfish: Ethereal aquatic chime dissolve
                    const chime = this.ctx.createOscillator();
                    const cGain = this.ctx.createGain();
                    chime.type = "sine";
                    chime.frequency.setValueAtTime(680, now);
                    chime.frequency.exponentialRampToValueAtTime(340, now + 0.25);
                    cGain.gain.setValueAtTime(0.24, now);
                    cGain.gain.exponentialRampToValueAtTime(0.001, now + 0.25);
                    chime.connect(cGain);
                    cGain.connect(this.masterGain);
                    chime.start(now);
                    chime.stop(now + 0.25);
                    break;
                }
                case 2: { // Sea Anemone / Barnacle: Viscous siphon suction release & liquid water jet squirt
                    if (this.noiseBuffer) {
                        const jet = this.ctx.createBufferSource();
                        jet.buffer = this.noiseBuffer;
                        const filter = this.ctx.createBiquadFilter();
                        filter.type = "lowpass";
                        filter.frequency.setValueAtTime(420, now);
                        filter.frequency.exponentialRampToValueAtTime(80, now + 0.18);
                        filter.Q.setValueAtTime(5.0, now);
                        const jGain = this.ctx.createGain();
                        jGain.gain.setValueAtTime(0.28, now);
                        jGain.gain.exponentialRampToValueAtTime(0.001, now + 0.18);
                        jet.connect(filter);
                        filter.connect(jGain);
                        jGain.connect(this.masterGain);
                        jet.start(now);
                        jet.stop(now + 0.18);
                    }
                    break;
                }
                case 3: { // Electric Eel / Shark: Underwater bio-electric spark cavitation burst
                    const zap = this.ctx.createOscillator();
                    const zGain = this.ctx.createGain();
                    zap.type = "sine";
                    zap.frequency.setValueAtTime(560, now);
                    zap.frequency.exponentialRampToValueAtTime(180, now + 0.22);
                    zGain.gain.setValueAtTime(0.26, now);
                    zGain.gain.exponentialRampToValueAtTime(0.001, now + 0.22);

                    // 60Hz ring modulation
                    const rm = this.ctx.createOscillator();
                    const rmGain = this.ctx.createGain();
                    rm.type = "square";
                    rm.frequency.setValueAtTime(60, now);
                    rmGain.gain.setValueAtTime(0.12, now);
                    rm.connect(rmGain);
                    rmGain.connect(zGain.gain);
                    rm.start(now);
                    rm.stop(now + 0.22);

                    zap.connect(zGain);
                    zGain.connect(this.masterGain);
                    zap.start(now);
                    zap.stop(now + 0.22);
                    break;
                }
                case 4: // Giant Crustacean / Megalodon: Deep ocean depth-charge shockwave & bubbling cauldron
                default: {
                    const boom = this.ctx.createOscillator();
                    const bGain = this.ctx.createGain();
                    boom.type = "sine";
                    boom.frequency.setValueAtTime(88, now);
                    boom.frequency.exponentialRampToValueAtTime(18, now + 0.50);
                    bGain.gain.setValueAtTime(0.40, now);
                    bGain.gain.exponentialRampToValueAtTime(0.001, now + 0.50);
                    boom.connect(bGain);
                    bGain.connect(this.masterGain);
                    boom.start(now);
                    boom.stop(now + 0.50);

                    if (this.noiseBuffer) {
                        const rumble = this.ctx.createBufferSource();
                        rumble.buffer = this.noiseBuffer;
                        const filter = this.ctx.createBiquadFilter();
                        filter.type = "lowpass";
                        filter.frequency.setValueAtTime(200, now);
                        const rGain = this.ctx.createGain();
                        rGain.gain.setValueAtTime(0.32, now);
                        rGain.gain.exponentialRampToValueAtTime(0.001, now + 0.50);
                        rumble.connect(filter);
                        filter.connect(rGain);
                        rGain.connect(this.masterGain);
                        rumble.start(now);
                        rumble.stop(now + 0.50);
                    }
                    break;
                }
            }
        },

        // Pickup Chime: bright two-tone retro arpeggio (587Hz -> 880Hz)
        playPickup: function () {
            if (!this.ctx) return;
            this.resume();
            const now = this.ctx.currentTime;

            if (window.gameInterop && (window.gameInterop.currentTheme.includes("space") || window.gameInterop.currentTheme.includes("invader"))) {
                // High-tech cosmic chime ping
                const osc = this.ctx.createOscillator();
                const gain = this.ctx.createGain();
                osc.type = "sine";
                osc.frequency.setValueAtTime(880.00, now); // A5
                osc.frequency.setValueAtTime(1318.51, now + 0.05); // E6
                gain.gain.setValueAtTime(0.20, now);
                gain.gain.exponentialRampToValueAtTime(0.001, now + 0.16);
                osc.connect(gain);
                gain.connect(this.masterGain);
                osc.start(now);
                osc.stop(now + 0.16);
                return;
            }

            if (window.gameInterop && (window.gameInterop.currentTheme.includes("underwater") || window.gameInterop.currentTheme.includes("ocean"))) {
                // Submerged crystal sea pearl ping
                const osc = this.ctx.createOscillator();
                const gain = this.ctx.createGain();
                osc.type = "sine";
                osc.frequency.setValueAtTime(587.33, now); // D5
                osc.frequency.setValueAtTime(880.00, now + 0.05); // A5
                gain.gain.setValueAtTime(0.22, now);
                gain.gain.exponentialRampToValueAtTime(0.001, now + 0.16);
                osc.connect(gain);
                gain.connect(this.masterGain);
                osc.start(now);
                osc.stop(now + 0.16);
                return;
            }

            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();

            osc.type = "square";
            osc.frequency.setValueAtTime(587.33, now); // D5
            osc.frequency.setValueAtTime(880.00, now + 0.05); // A5

            gain.gain.setValueAtTime(0.20, now);
            gain.gain.exponentialRampToValueAtTime(0.001, now + 0.12);

            osc.connect(gain);
            gain.connect(this.masterGain);

            osc.start(now);
            osc.stop(now + 0.12);
        },

        // Player Damage: harsh low-frequency buzz (120Hz -> 50Hz)
        playPlayerDamage: function () {
            if (!this.ctx) return;
            this.resume();
            const now = this.ctx.currentTime;
            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();

            osc.type = "sawtooth";
            osc.frequency.setValueAtTime(120, now);
            osc.frequency.exponentialRampToValueAtTime(50, now + 0.15);

            gain.gain.setValueAtTime(0.28, now);
            gain.gain.exponentialRampToValueAtTime(0.001, now + 0.15);

            osc.connect(gain);
            gain.connect(this.masterGain);

            osc.start(now);
            osc.stop(now + 0.15);
        },

        // Player Die: classic sad descending 4-note retro melody ("wah-wah-wah-waaaah")
        playPlayerDie: function () {
            if (!this.ctx) return;
            this.resume();
            const now = this.ctx.currentTime;
            const notes = [
                { freq: 392.00, start: 0.00, dur: 0.18, endFreq: 375.00 }, // G4
                { freq: 369.99, start: 0.20, dur: 0.18, endFreq: 355.00 }, // F#4
                { freq: 349.23, start: 0.40, dur: 0.18, endFreq: 335.00 }, // F4
                { freq: 329.63, start: 0.60, dur: 0.55, endFreq: 220.00 }  // E4 -> sad slide down
            ];

            for (let i = 0; i < notes.length; i++) {
                const n = notes[i];
                const osc = this.ctx.createOscillator();
                const gain = this.ctx.createGain();

                osc.type = "sawtooth";
                osc.frequency.setValueAtTime(n.freq, now + n.start);
                osc.frequency.linearRampToValueAtTime(n.endFreq, now + n.start + n.dur);

                gain.gain.setValueAtTime(0.24, now + n.start);
                gain.gain.exponentialRampToValueAtTime(0.001, now + n.start + n.dur);

                osc.connect(gain);
                gain.connect(this.masterGain);

                osc.start(now + n.start);
                osc.stop(now + n.start + n.dur);
            }
        },

        dispatchFlags: function (flags) {
            if (!flags || this.isMuted) return;
            if (flags & 1) this.playPlayerShoot();
            if (flags & 2) this.playStep(1.0);
            if (flags & 4) this.playStep(1.4); // 50% accelerated run step
            if (flags & 8) this.playPlayerJump();
            if (flags & 16) this.playMonsterShoot();
            if (flags & 32) {
                let playedSpecific = false;
                if (flags & 512) { this.playMonsterTypeDie(0); playedSpecific = true; }
                else if (flags & 1024) { this.playMonsterTypeDie(1); playedSpecific = true; }
                else if (flags & 2048) { this.playMonsterTypeDie(2); playedSpecific = true; }
                else if (flags & 4096) { this.playMonsterTypeDie(3); playedSpecific = true; }
                else if (flags & 8192) { this.playMonsterTypeDie(4); playedSpecific = true; }
                if (!playedSpecific) {
                    this.playMonsterDie();
                }
            }
            if (flags & 64) this.playPickup();
            if (flags & 128) this.playPlayerDamage();
            if (flags & 256) this.playPlayerDie();
        }
    }
};
