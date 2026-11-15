// What a practice is beyond its name and length: its nomenclator code, the equipment it needs and what it uses up.
export interface PracticeSupplyDto {
  supplyId: string;
  supply: string;
  unit: string;
  quantity: number;
}

export interface PracticeSupplyCommand {
  supplyId: string;
  quantity: number;
}

export interface SavePracticeCommand {
  code?: string | null;
  equipmentId?: string | null;
  supplies: PracticeSupplyCommand[];
}
