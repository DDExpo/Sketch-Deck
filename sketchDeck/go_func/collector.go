package go_func

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"time"
)

func MakeCollection(pathImages string) ([]ImagesWithThumbnails, error) {

	var imageCollection []ImagesWithThumbnails

	validPaths, err := validatePaths(pathImages)

	if err != nil {
		return imageCollection, fmt.Errorf("error: \n %v", err)
	}

	for _, validPath := range validPaths {
		fmt.Println(validPath)
		fileDate := validPath[1]
		fileName := validPath[0]

		imageEntry := ImagesWithThumbnails{Image: filepath.Join("../frontend/static/fullImages", fileName), Name: fileName, Date: fileDate}
		for _, thumbSizeType := range ThumbSizesTypes {
			imageEntry.Thumbs = map[string]string{thumbSizeType: filepath.Join("../frontend/static/thumbnails", thumbSizeType, fileName)}
			imageCollection = append(imageCollection, imageEntry)
		}
	}
	return imageCollection, err
}

func validatePaths(pathImages string) ([][2]string, error) {

	var imageExtensions = map[string]struct{}{
		".jpg":  {},
		".jpeg": {},
		".png":  {},
	}

	var entries [][2]string
	metaInfo, err := os.Stat(pathImages)

	if os.IsNotExist(err) {
		return [][2]string{}, fmt.Errorf("file/directory doesnt exists \n %v", err)
	} else if err != nil {
		return [][2]string{}, fmt.Errorf("error with given file \n %v", err)
	}

	if metaInfo.IsDir() {
		dirEntries, err := os.ReadDir(pathImages)

		if err != nil {
			return [][2]string{}, fmt.Errorf("error reading directory \n %v", err)
		}

		for _, direntry := range dirEntries {
			var entryDate string

			if date, err := direntry.Info(); err != nil {
				entryDate = time.Now().Format("2006-01-02T15:04")
			} else {
				entryDate = date.ModTime().Format("2006-01-02T15:04")
			}

			entryName := direntry.Name()
			dotIndex := strings.LastIndex(entryName, ".")
			ext := strings.ToLower(entryName[dotIndex:])
			if _, ok := imageExtensions[ext]; ok {
				entries = append(entries, [2]string{entryName, entryDate})
			}
		}
	} else {
		entryName := metaInfo.Name()
		dotIndex := strings.LastIndex(entryName, ".")
		ext := strings.ToLower(entryName[dotIndex:])
		if _, ok := imageExtensions[ext]; ok {
			entries = append(entries, [2]string{entryName, metaInfo.ModTime().Format("2006-01-02T15:04")})
		}
	}
	return entries, err
}
