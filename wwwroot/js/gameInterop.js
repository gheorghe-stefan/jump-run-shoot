// JumpRunShoot - Canvas & Input Interop
window.gameInterop = {
    canvas: null,
    ctx: null,
    imageData: null,
    dotNetHelper: null,
    animFrameId: null,
    isRunning: false,

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
        return true;
    },

    setupKeyboard: function () {
        const self = this;

        window.addEventListener("keydown", function (e) {
            if (self.preventKeys.has(e.code) || (e.altKey && !e.ctrlKey)) {
                e.preventDefault();
            }
            if (self.dotNetHelper) {
                self.dotNetHelper.invokeMethodAsync("OnKeyDown", e.code);
            }
        }, { passive: false });

        window.addEventListener("keyup", function (e) {
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

    renderFrame: function (pixelBytes) {
        if (!this.ctx || !this.imageData) return;
        this.imageData.data.set(pixelBytes);
        this.ctx.putImageData(this.imageData, 0, 0);
    },

    focusCanvas: function () {
        if (this.canvas) {
            this.canvas.focus();
        }
    }
};
