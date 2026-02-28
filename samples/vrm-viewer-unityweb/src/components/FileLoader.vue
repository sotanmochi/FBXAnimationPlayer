<template>
  <div class="file-loader">
    <!-- input は非表示。ボタンのクリックで .click() をトリガーする。
         iOS Safari では input.click() をユーザーアクション内（同期）で呼ぶ必要がある。 -->
    <input
      ref="vrmInputRef"
      type="file"
      accept=".vrm"
      class="hidden-input"
      @change="onVrmSelected"
    />
    <input
      ref="fbxInputRef"
      type="file"
      accept=".fbx"
      class="hidden-input"
      @change="onFbxSelected"
    />

    <button
      class="btn"
      :disabled="!isUnityReady || vrm.isLoading.value"
      @click="vrmInputRef?.click()"
    >
      {{ vrm.isLoading.value ? '読み込み中...' : 'VRM を読み込む' }}
    </button>

    <button
      class="btn"
      :disabled="!vrm.isLoaded.value || anim.isLoading.value"
      @click="fbxInputRef?.click()"
    >
      {{ anim.isLoading.value ? '読み込み中...' : 'FBX を読み込む' }}
    </button>

    <p v-if="vrm.error.value" class="error-text">VRM: {{ vrm.error.value }}</p>
    <p v-if="anim.error.value" class="error-text">FBX: {{ anim.error.value }}</p>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'

const props = defineProps<{
  isUnityReady: boolean
  vrm: { isLoaded: { value: boolean }; isLoading: { value: boolean }; error: { value: string | null }; loadFile: (f: File) => void }
  anim: { isLoaded: { value: boolean }; isLoading: { value: boolean }; error: { value: string | null }; loadFile: (f: File) => void }
}>()

const vrmInputRef = ref<HTMLInputElement>()
const fbxInputRef = ref<HTMLInputElement>()

function onVrmSelected(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (file) {
    props.vrm.loadFile(file)
    // 同一ファイルを再選択できるよう value をリセット
    ;(e.target as HTMLInputElement).value = ''
  }
}

function onFbxSelected(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (file) {
    props.anim.loadFile(file)
    ;(e.target as HTMLInputElement).value = ''
  }
}
</script>

<style scoped>
.file-loader {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.hidden-input {
  display: none;
}

.btn {
  padding: 8px 16px;
  background: #40c4a8;
  color: #0c0e14;
  border: none;
  border-radius: 6px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: opacity 0.15s;
  white-space: nowrap;
}

.btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.btn:not(:disabled):hover {
  opacity: 0.85;
}

.error-text {
  color: #ff6b6b;
  font-size: 12px;
  width: 100%;
}
</style>
