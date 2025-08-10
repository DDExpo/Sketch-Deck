<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { Collector, OpenDialogFileFullPath } from "../../wailsjs/go/main/App.js";
import { go_func } from '../../wailsjs/go/models.js';
import DragAndDropArea from './DragAndDropArea.vue';

const testPath = import.meta.env.VITE_TEST_PATH;

const viewMode = ref<string>('details-view');
const searchInput = ref<string>('');

const path = ref<string>(testPath);
const collection = ref<go_func.ImagesWithThumbnails[]>([]);

const sortBy = ref<'name' | 'date' | ''>('');
const sortByAscDsc = ref<'↑' | '↓' | ''>('')
const draggedIndex = ref<number | null>(null);
const hoveredItem = ref<number | null>(null);


const filteredCollection = computed(() => {

  let middleCollection = collection.value
  
  if (searchInput.value || sortBy.value) {
    middleCollection = [...collection.value]
    if (sortBy.value){
      if (sortByAscDsc.value == '↑') {
        if (sortBy.value == 'name') {
          middleCollection.sort((a, b) => a.Name.localeCompare(b.Name))
        } else {
          middleCollection.sort((a, b) => a.Date.localeCompare(b.Date))}
        } else if (sortByAscDsc.value == '↓') {
          if (sortBy.value == 'name') {
            middleCollection.reverse()
          }}}
    if (searchInput.value) {
      middleCollection = middleCollection.filter(
        item => item.Name.toLowerCase().includes(searchInput.value.toLowerCase())
      )}}
  return middleCollection
});

async function openFileDialog() {
  const newpath = await OpenDialogFileFullPath()
  if (!newpath) return
  path.value = newpath
};

function toggleSort(column: 'name' | 'date') {
  if (sortBy.value !== column) {
    sortByAscDsc.value = '↑'
    sortBy.value = column
  } else {
    if (sortByAscDsc.value == '↑' ) {
      sortByAscDsc.value = '↓'
    } else if (sortByAscDsc.value == '↓') {
      sortByAscDsc.value = ''
      sortBy.value = ''
    }
  }
}

watch(path, async (newpath) => {
  if (!newpath) {return}
  try {
    console.info("start collecting images... ")
    const result = await Collector(newpath)
    collection.value = collection.value.concat(result)
    console.info("end Succsessfull")
    path.value = ''
  } catch (error) {
    console.error(error)
  }
}, { immediate: true });

function onDragStart(index: number) {
  draggedIndex.value = index
};

function onDragEnter(targetIndex: number) {
  if (draggedIndex.value === null || draggedIndex.value === targetIndex) return
  const draggedItem = collection.value[draggedIndex.value]

  collection.value.splice(draggedIndex.value, 1)
  collection.value.splice(targetIndex, 0, draggedItem)
  draggedIndex.value = targetIndex
};

function deleteItem(index: number) {
  collection.value.splice(index, 1)
};

</script>

<template>
<div class="explorer-container">
  <div class="explorer-header">
    <button type="button" class="add-button" @click="openFileDialog">Add</button>
    <select id="options" class="dark-select" v-model="viewMode">
      <option id="options-1" value="small-view">Small</option>
      <option id="options-2" value="medium-view">Medium</option>
      <option id="options-3" value="large-view">Large</option>
      <option id="options-4" value="gigantic-view">Gigantic</option>
      <option id="options-5" value="details-view">Details</option>
    </select>
  </div>
  <input id="search-table" class="explorer-search" type="search" placeholder="search" v-model="searchInput"></input>
  <div v-if="viewMode === 'details-view'" class="filter-bar">
    <button class="filter-button" @click="toggleSort('name')">Name <span v-if="sortBy == 'name'">{{ sortByAscDsc }}</span></button>
    <button class="filter-button" @click="toggleSort('date')">Date <span v-if="sortBy == 'date'">{{ sortByAscDsc }}</span></button>
  </div>
  <div :class="viewMode" style="display: flex; position: relative; flex-wrap: wrap; gap: 6px;">
    <DragAndDropArea @new-path="path = $event" />
    <div class="explorer-item" v-for="(item, index) in filteredCollection" :key="index" draggable="true"
    @dragstart="onDragStart(index)" @drop="draggedIndex = null" @dragover.prevent @dragenter.prevent="onDragEnter(index)"
    @mouseout="draggedIndex = null" @mouseenter="hoveredItem = index" @mouseleave="hoveredItem = null"
    >
      <img class="explorer-icon" :src="item.Image">
        <button v-if="hoveredItem === index" @click="deleteItem(index)" class="delete-button">X</button>
      </img>
      <div class="explorer-name"><input :id="'item-name-'+index" type="text" v-model.lazy="item.Name" style="all: unset; width: 100%;"/></div>
      <div class="explorer-date" v-if="viewMode === 'details-view'">{{ item.Date }}</div>
    </div>
  </div>
</div>
</template>

<style scoped>

.explorer-container {
  display: flex;
  flex-direction: column;
  background: #1e1e1e;
  color: #ddd;
  border: 1.5px solid #333;
  padding: 15px;
  font-family: "Segoe UI", Tahoma, Geneva, Verdana, sans-serif;
  border-radius: 20px;
}

.explorer-header {
  display: grid;
  justify-content: end;
  margin-bottom: 10px;
  align-items: flex-end;
  grid-auto-flow: column;
  gap: 5px;
}

.add-button {
  display: flex;
  background-color: #2d2d2d;
  color: #f0f0f0;
  border: 1px solid #555;
  border-radius: 4px;
  padding: 3px 4px;
  font-family: "Segoe UI", Tahoma, Geneva, Verdana, sans-serif;
  font-size: 12px;
  text-align: center;
  outline: none;
  appearance: none;
  transition: background-color 0.2s, border-color 0.2s;  
}

.add-button:hover {
  background-color: #3a3a3a;
  border-color: #888;
}

.add-button:focus {
  border-color: #0078d7;
  box-shadow: 0 0 0 2px rgba(0, 120, 215, 0.3);
}

.dark-select {
  display: flex;
  background-color: #2d2d2d;
  color: #f0f0f0;
  border: 1px solid #555;
  border-radius: 4px;
  padding: 3px 4px;
  font-family: "Segoe UI", Tahoma, Geneva, Verdana, sans-serif;
  font-size: 12px;
  text-align: center;
  outline: none;
  appearance: none;
  transition: background-color 0.2s, border-color 0.2s;
}

.dark-select:hover {
  background-color: #3a3a3a;
  border-color: #888;
}

.dark-select:focus {
  border-color: #0078d7;
  box-shadow: 0 0 0 2px rgba(0, 120, 215, 0.3);
}

.explorer-search {
  all: unset;
  box-sizing: border-box;
  margin-bottom: 5px;
  width: 100%;
  padding: 5px 10px;
  background: #2d2d2d;
  border-bottom: 1px solid #444;
  border-radius: 10px;
  font-weight: bold;
}

.filter-bar {
  display: flex;
  justify-content: space-between;
  gap: 1em;
  padding: 5px;
}

.filter-button {
  background: transparent;
  border: none;
  color: white;
  font-size: 16px;
  font-weight: bold;
  cursor: pointer;
  padding: 0;
  margin: 0;
  line-height: 1;
}

.filter-button:hover {
  text-shadow: 0 0 10px white;
}
.explorer-item {
  position: relative;
  padding-top: 5px;
}

.explorer-item:hover {
  border: 1.5px solid #3a94e0;
  background: #333b44;
}

.explorer-icon {
  width: 64px;
  height: 64px;
  object-fit: contain;
  filter: brightness(0.95);
}

.explorer-name {
  font-size: 12px;
  margin-left: 3px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  color: #e0e0e0;
}

.explorer-date {
  font-size: 10px;
  color: #888;
}

.delete-button {
  position: absolute;
  top: -8px;
  right: -9px;
  background: #3a94e0;
  color:white;
  border: #3a94e0;
  border-radius: 40%;
  width: 17px;
  height: 17px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  padding: 1px;
  font-size: 10px;
  line-height: 1;
  z-index: 10;
}

.small-view .explorer-item {
  width: 92px;
}

.small-view .explorer-icon {
  width: 90px;
  height: 90px;
}

.medium-view .explorer-item {
  width: 152px;
}

.medium-view .explorer-icon {
  width: 150px;
  height: 150px;
}

.large-view .explorer-item {
  width: 228px;
}

.large-view .explorer-icon {
  width: 226px;
  height: 226px;
}

.gigantic-view .explorer-item {
  width: 606px;
}

.gigantic-view .explorer-icon {
  width: 600px;
  height: 600px;
}

.details-view {
  flex-direction: column;
  border-radius: 5px;
  margin-right: 18px;
  
}

.details-view .explorer-item {
  width: 100%;
  display: flex;
  align-items: center;
  padding: 4px 8px;
}

.details-view .explorer-icon {
  width: 26px;
  height: 26px;
}

.details-view .explorer-name {
  flex: 1;
  text-align: left;
}

.details-view .explorer-date {
  width: 100px;
  text-align: right;
}

input[type="search"]::-webkit-search-cancel-button {
  display: none;
  -webkit-appearance: none;
}

</style>