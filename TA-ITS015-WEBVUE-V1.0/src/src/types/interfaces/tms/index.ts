// Action của item trên map
export interface IMapItemAction {
  action: 'click' | 'mouseover' | 'dblclick' | 'rightclick' | 'mouseout';
  itemId: string;
  itemGroup: string;
  itemData?: any;
  geometry?: {
    type: string;
    coordinates: any;
  };
}