<template>
  <Transition name="fade">
    <div v-if="visible" class="overlay">
      <div class="content">
        <div class="spinner" />
        <p class="message">{{ message }}</p>
        <div v-if="progress != null && progress > 0" class="progress-bar">
          <div class="progress-fill" :style="{ width: `${progress! * 100}%` }" />
        </div>
      </div>
    </div>
  </Transition>
</template>

<script setup lang="ts">
defineProps<{
  visible: boolean
  message?: string
  progress?: number  // 0.0 〜 1.0
}>()
</script>

<style scoped>
.overlay {
  position: absolute;
  inset: 0;
  background: rgba(12, 14, 20, 0.85);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 10;
}

.content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 3px solid rgba(64, 196, 168, 0.3);
  border-top-color: #40c4a8;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

.message {
  color: rgba(255, 255, 255, 0.8);
  font-size: 14px;
}

.progress-bar {
  width: 200px;
  height: 4px;
  background: rgba(255, 255, 255, 0.15);
  border-radius: 2px;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: #40c4a8;
  border-radius: 2px;
  transition: width 0.1s ease;
}

.fade-enter-active,
.fade-leave-active { transition: opacity 0.3s; }
.fade-enter-from,
.fade-leave-to     { opacity: 0; }

@keyframes spin {
  to { transform: rotate(360deg); }
}
</style>
