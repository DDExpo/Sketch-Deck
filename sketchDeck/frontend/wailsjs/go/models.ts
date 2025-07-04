export namespace go_func {
	
	export class ImagesWithThumbnails {
	    Image: string;
	    Name: string;
	    Date: string;
	    Thumbs: Record<string, string>;
	
	    static createFrom(source: any = {}) {
	        return new ImagesWithThumbnails(source);
	    }
	
	    constructor(source: any = {}) {
	        if ('string' === typeof source) source = JSON.parse(source);
	        this.Image = source["Image"];
	        this.Name = source["Name"];
	        this.Date = source["Date"];
	        this.Thumbs = source["Thumbs"];
	    }
	}

}

