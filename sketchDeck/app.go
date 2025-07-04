package main

import (
	"context"
	"sketchDeck/go_func"

	"github.com/wailsapp/wails/v2/pkg/runtime"
)

// App struct
type App struct {
	ctx context.Context
}

// NewApp creates a new App application struct
func NewApp() *App {
	return &App{}
}

// startup is called when the app starts. The context is saved
// so we can call the runtime methods
func (a *App) startup(ctx context.Context) {
	a.ctx = ctx
}

func (a *App) OpenDialogFileFullPath() (string, error) {
	return runtime.OpenDirectoryDialog(a.ctx, runtime.OpenDialogOptions{
		Title: "Select Folder to add",
	})
}

func (a *App) Collector(path string) ([]go_func.ImagesWithThumbnails, error) {
	collection, err := go_func.MakeCollection(path)
	return collection, err
}
