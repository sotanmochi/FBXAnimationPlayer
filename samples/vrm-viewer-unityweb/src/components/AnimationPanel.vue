<template>
  <div class="animation-panel" :class="{ disabled: !anim.isLoaded.value }">
    <!-- 再生コントロール -->
    <div class="controls">
      <button class="ctrl-btn" :disabled="!anim.isLoaded.value" @click="anim.stop()">
        ■
      </button>
      <button class="ctrl-btn play-btn" :disabled="!anim.isLoaded.value" @click="anim.togglePlay()">
        {{ anim.isPlaying.value ? '⏸' : '▶' }}
      </button>
    </div>

    <!-- シークバー -->
    <div class="seek-area">
      <input
        type="range"
        class="seek-bar"
        min="0"
        max="1"
        step="0.001"
        :value="anim.normalizedTime.value"
        :disabled="!anim.isLoaded.value"
        @input="onSeek"
        @mousedown="onSeekStart"
        @touchstart="onSeekStart"
        @mouseup="onSeekEnd"
        @touchend="onSeekEnd"
      />
      <span class="time-label">
        {{ formatTime(anim.currentTime.value) }} / {{ formatTime(anim.duration.value) }}
      </span>
    </div>

    <!-- ループ・速度 -->
    <div class="options">
      <label class="option-label">
        <input
          type="checkbox"
          :checked="anim.isLooping.value"
          :disabled="!anim.isLoaded.value"
          @change="onLoopChange"
        />
        ループ
      </label>

      <label class="option-label">
        速度
        <select
          :value="anim.speed.value"
          :disabled="!anim.isLoaded.value"
          @change="onSpeedChange"
        >
          <option value="0.25">0.25x</option>
          <option value="0.5">0.5x</option>
          <option value="1">1.0x</option>
          <option value="1.5">1.5x</option>
          <option value="2">2.0x</option>
        </select>
      </label>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { useAnimationController } from '../composables/useAnimationController'

const props = defineProps<{
  anim: ReturnType<typeof useAnimationController>
}>()

let isSeeking = false

function onSeekStart() { isSeeking = true }
function onSeekEnd()   { isSeeking = false }

function onSeek(e: Event) {
  if (!isSeeking) return
  const v = parseFloat((e.target as HTMLInputElement).value)
  props.anim.seek(v)
}

function onLoopChange(e: Event) {
  props.anim.setLooping((e.target as HTMLInputElement).checked)
}

function onSpeedChange(e: Event) {
  props.anim.setSpeed(parseFloat((e.target as HTMLSelectElement).value))
}

function formatTime(seconds: number): string {
  const m = Math.floor(seconds / 60)
  const s = (seconds % 60).toFixed(2).padStart(5, '0')
  return `${m}:${s}`
}
</script>

<style scoped>
.animation-panel {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
  padding: 8px 12px;
  background: rgba(12, 14, 20, 0.88);
  border-top: 1px solid rgba(255, 255, 255, 0.08);
}

.animation-panel.disabled {
  opacity: 0.5;
  pointer-events: none;
}

.controls {
  display: flex;
  gap: 6px;
}

.ctrl-btn {
  width: 36px;
  height: 36px;
  border: 1px solid rgba(255, 255, 255, 0.2);
  background: transparent;
  color: #fff;
  border-radius: 6px;
  font-size: 16px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.15s;
}

.ctrl-btn:not(:disabled):hover { background: rgba(255, 255, 255, 0.1); }
.ctrl-btn:disabled { opacity: 0.4; cursor: not-allowed; }

.play-btn { color: #40c4a8; border-color: #40c4a8; }

.seek-area {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 160px;
}

.seek-bar {
  flex: 1;
  accent-color: #40c4a8;
  height: 4px;
  cursor: pointer;
}

.time-label {
  color: rgba(255, 255, 255, 0.7);
  font-size: 12px;
  white-space: nowrap;
  font-variant-numeric: tabular-nums;
}

.options {
  display: flex;
  gap: 12px;
  align-items: center;
}

.option-label {
  display: flex;
  align-items: center;
  gap: 4px;
  color: rgba(255, 255, 255, 0.7);
  font-size: 13px;
  cursor: pointer;
}

select {
  background: rgba(255, 255, 255, 0.08);
  color: #fff;
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: 4px;
  padding: 2px 6px;
  font-size: 13px;
}
</style>
