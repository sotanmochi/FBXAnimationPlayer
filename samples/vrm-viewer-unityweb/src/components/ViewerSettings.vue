<template>
  <div class="viewer-settings">
    <button class="settings-toggle" @click="isOpen = !isOpen" title="Settings">
      &#x2699;
    </button>

    <div v-if="isOpen" class="settings-dropdown">
      <div class="settings-title">Settings</div>

      <!-- Background Color -->
      <div class="settings-section">
        <div class="settings-label">Background Color</div>
        <div
          class="color-preview"
          :style="{ backgroundColor: previewCss }"
        />
        <label class="slider-row">
          <span class="slider-label slider-r">R</span>
          <input
            type="range"
            class="color-range range-r"
            min="0"
            max="1"
            step="0.01"
            :value="settings.bgColor.value.r"
            @input="onColorInput('r', $event)"
          />
          <span class="slider-value">{{ settings.bgColor.value.r.toFixed(2) }}</span>
        </label>
        <label class="slider-row">
          <span class="slider-label slider-g">G</span>
          <input
            type="range"
            class="color-range range-g"
            min="0"
            max="1"
            step="0.01"
            :value="settings.bgColor.value.g"
            @input="onColorInput('g', $event)"
          />
          <span class="slider-value">{{ settings.bgColor.value.g.toFixed(2) }}</span>
        </label>
        <label class="slider-row">
          <span class="slider-label slider-b">B</span>
          <input
            type="range"
            class="color-range range-b"
            min="0"
            max="1"
            step="0.01"
            :value="settings.bgColor.value.b"
            @input="onColorInput('b', $event)"
          />
          <span class="slider-value">{{ settings.bgColor.value.b.toFixed(2) }}</span>
        </label>
      </div>

      <!-- Field of View -->
      <div class="settings-section">
        <div class="settings-label">Field of View</div>
        <label class="slider-row">
          <span class="slider-label slider-fov">FoV</span>
          <input
            type="range"
            class="fov-range"
            min="10"
            max="120"
            step="1"
            :value="settings.fov.value"
            @input="onFoVInput"
          />
          <span class="slider-value">{{ settings.fov.value }}&deg;</span>
        </label>
      </div>

      <!-- Camera Reset -->
      <div class="settings-section">
        <button class="reset-camera-btn" @click="settings.resetCamera()">
          Reset Camera
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import type { ViewerSettings } from '../composables/useViewerSettings'

const props = defineProps<{
  settings: ViewerSettings
}>()

const isOpen = ref(false)

const previewCss = computed(() => {
  const { r, g, b } = props.settings.bgColor.value
  return `rgb(${Math.round(r * 255)}, ${Math.round(g * 255)}, ${Math.round(b * 255)})`
})

function onColorInput(channel: 'r' | 'g' | 'b', e: Event) {
  const val = parseFloat((e.target as HTMLInputElement).value)
  const c = { ...props.settings.bgColor.value }
  c[channel] = val
  props.settings.setBackgroundColor(c.r, c.g, c.b)
}

function onFoVInput(e: Event) {
  const val = parseFloat((e.target as HTMLInputElement).value)
  props.settings.setFoV(val)
}

</script>

<style scoped>
.viewer-settings {
  position: relative;
}

.settings-toggle {
  width: 36px;
  height: 36px;
  border: 1px solid rgba(255, 255, 255, 0.15);
  background: transparent;
  color: rgba(255, 255, 255, 0.7);
  border-radius: 6px;
  font-size: 20px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.15s;
}

.settings-toggle:hover {
  background: rgba(255, 255, 255, 0.1);
  border-color: rgba(255, 255, 255, 0.3);
}

.settings-dropdown {
  position: absolute;
  top: calc(100% + 8px);
  right: 0;
  width: 280px;
  padding: 14px;
  background: rgba(12, 14, 20, 0.95);
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 8px;
  z-index: 100;
}

.settings-title {
  font-size: 14px;
  font-weight: 600;
  color: #e6e6e6;
  margin-bottom: 10px;
}

.settings-section {
  margin-bottom: 10px;
}

.settings-label {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.6);
  margin-bottom: 6px;
}

.color-preview {
  width: 100%;
  height: 24px;
  border-radius: 4px;
  border: 1px solid rgba(255, 255, 255, 0.2);
  margin-bottom: 6px;
}

.slider-row {
  display: flex;
  align-items: center;
  gap: 6px;
  margin: 4px 0;
  cursor: pointer;
}

.slider-label {
  font-size: 12px;
  width: 14px;
  text-align: center;
  font-weight: 600;
}

.slider-r { color: #dc5050; }
.slider-g { color: #50c850; }
.slider-b { color: #5078dc; }
.slider-fov { color: rgba(255, 255, 255, 0.6); }

.color-range {
  flex: 1;
  height: 4px;
  cursor: pointer;
}

.range-r { accent-color: #dc5050; }
.range-g { accent-color: #50c850; }
.range-b { accent-color: #5078dc; }

.fov-range {
  flex: 1;
  height: 4px;
  cursor: pointer;
  accent-color: #40c4a8;
}

.slider-value {
  font-size: 11px;
  color: rgba(255, 255, 255, 0.5);
  width: 30px;
  text-align: right;
  font-variant-numeric: tabular-nums;
}

.reset-camera-btn {
  width: 100%;
  height: 32px;
  border: 1px solid rgba(255, 255, 255, 0.28);
  background: transparent;
  color: rgba(255, 255, 255, 0.8);
  border-radius: 6px;
  font-size: 13px;
  cursor: pointer;
  transition: background 0.12s, border-color 0.12s;
}

.reset-camera-btn:hover {
  background: rgba(255, 255, 255, 0.08);
  border-color: rgba(255, 255, 255, 0.45);
}

.reset-camera-btn:active {
  background: rgba(255, 255, 255, 0.16);
}
</style>
