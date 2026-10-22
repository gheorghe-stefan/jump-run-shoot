// JumpRunShoot - Canvas & Input Interop
window.gameInterop = {
    canvas: null,
    ctx: null,
    imageData: null,
    dotNetHelper: null,
    animFrameId: null,
    isRunning: false,
    currentTheme: "duke",

    setTheme: function (themeName) {
        this.currentTheme = (themeName || "").toLowerCase();
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
        init: function () {
            if (!this.audio) {
                this.audio = new Audio("audio/retro_bgm.wav");
                this.audio.loop = true;
                this.audio.volume = 0.35;
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

        // Pickup Chime: bright two-tone retro arpeggio (587Hz -> 880Hz)
        playPickup: function () {
            if (!this.ctx) return;
            this.resume();
            const now = this.ctx.currentTime;

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
            if (flags & 32) this.playMonsterDie();
            if (flags & 64) this.playPickup();
            if (flags & 128) this.playPlayerDamage();
            if (flags & 256) this.playPlayerDie();
        }
    }
};
