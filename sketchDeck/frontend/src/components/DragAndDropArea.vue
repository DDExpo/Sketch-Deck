<script setup lang="ts">
import { ref } from 'vue';

const isDragging = ref(false)
const dynamicZIndex = ref(0)

const emit = defineEmits<{(e: 'new-path', path: string): void}>()

function isFileDrag(e: DragEvent): boolean {
  return Array.from(e.dataTransfer?.types || []).includes('Files')
}

function onDragLeave(e: DragEvent) {
  if (!isFileDrag(e)) return 
  e.preventDefault()
  isDragging.value = false
  dynamicZIndex.value = 0
}

function onDragEnter(e: DragEvent) {
    if (!isFileDrag(e)) return 
    e.preventDefault()
    isDragging.value = true
    dynamicZIndex.value = 1
}

function onDrop(e: DragEvent) {
  if (!isFileDrag(e)) return 
  e.preventDefault()
  isDragging.value = false
  dynamicZIndex.value = 0

  const files = e.dataTransfer?.files
  if (files && (files.length > 0)) {
    if (files.length > 1) {
      // ss
    }
    const fileOrFolder = files[0]
    console.error((fileOrFolder as any).path)
    emit('new-path', (fileOrFolder as any).path)
  }
}

</script>

<template>
  <div class="drop-layer" :style="{ zIndex: dynamicZIndex }" @dragenter="onDragEnter" @drop="onDrop" @dragleave="onDragLeave" @dragover.prevent>
    <div class="drop-overlay" v-if="isDragging">
        <div class="drop-message">Drop to add</div>
    </div>
  </div>
</template>

<style scoped>

.drop-layer {
  position: absolute;
  inset: 0;
  min-height: 300px;
  pointer-events: auto;
}

.drop-overlay {
  display: flex;
  position: absolute;
  inset: 0;
  z-index: 1;
  background: rgba(30, 30, 30, 0.5);
  color: white;
  align-items: center;
  justify-content: center;
  pointer-events: none;
  border: 2px dashed #3a94e0;
}

.drop-message {
  font-size: 26px;
  background: #222;
  padding: 15px;
  border-radius: 10px;
  border: 2px dashed #aaa;
}

</style>
