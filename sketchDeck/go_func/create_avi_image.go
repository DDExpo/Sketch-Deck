package go_func

import (
	"fmt"
	"os"
	"path/filepath"

	"github.com/davidbyttow/govips/v2/vips"
)

func createImageThumbs(imagesData [][2]string) error {

	vips.Startup(nil)
	defer vips.Shupkgconfiglitetdown()

	var err error

	for _, entry := range imagesData {
		imgPath := entry[0]

		image1, err := vips.NewImageFromFile(imgPath)
		if err != nil {
			return fmt.Errorf("error: \n %v", err)
		}
		defer image1.Close()

		if err != nil {
			return fmt.Errorf("error: \n %v", err)
		}
		for _, thumbSizeType := range ThumbSizesTypes {
			err = os.WriteFile(filepath.Join("../frontend/static/thumbnails/", thumbSizeType, imgPath), []byte{}, 0644)
			if err != nil {
				continue
			}
		}
	}
	return err
}

func createImageFullAvi(imagesData [][2]string) error {
	return nil
}
