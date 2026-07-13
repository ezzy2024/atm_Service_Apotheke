/**
 * Mock Implementation for TI-as-a-Service (TIaaS) Integration
 * 
 * Provider Target: e-health-tec / medisign / RISE
 * This service simulates the REST API calls to a cloud-based TI connector.
 * It provides methods to read eGK (elektronische Gesundheitskarte), 
 * verify e-prescriptions (E-Rezept), and send/receive KIM messages.
 */

export interface PatientTIInfo {
  kvnr: string;
  firstName: string;
  lastName: string;
  birthDate: string;
  healthInsurance: string;
  ikNumber: string;
  status: string;
}

export class TIService {
  private apiKey: string;
  private endpoint: string;

  constructor() {
    this.apiKey = import.meta.env.VITE_TI_API_KEY || 'mock-key';
    this.endpoint = import.meta.env.VITE_TI_ENDPOINT || 'https://api.mock-tiaas.de/v1';
  }

  /**
   * Simulates a request to a local/networked eGK Card Terminal via the TIaaS Provider.
   * In a real scenario, this triggers the card terminal to prompt the user to insert their card.
   */
  async readPatientCard(terminalId: string): Promise<PatientTIInfo> {
    console.log(`[TIaaS] Triggering card read on terminal ${terminalId}...`);
    
    // Simulate network delay
    await new Promise(resolve => setTimeout(resolve, 2000));

    // Mock successful card read
    return {
      kvnr: "A123456789",
      firstName: "Max",
      lastName: "Hassan",
      birthDate: "1980-05-15",
      healthInsurance: "AOK Plus",
      ikNumber: "109900019",
      status: "1000" // Valid
    };
  }

  /**
   * Simulates verifying and retrieving an E-Rezept (e-prescription) from the Fachdienst.
   */
  async fetchERezept(prescriptionId: string) {
    console.log(`[TIaaS] Fetching E-Rezept ${prescriptionId}...`);
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    return {
      id: prescriptionId,
      status: "VERIFIED",
      medication: [
        { pzn: "01234567", name: "Ibuprofen 400mg", quantity: 1 }
      ],
      doctor: "Dr. med. Hans Beispiel",
      issuedAt: new Date().toISOString()
    };
  }

  /**
   * Simulates sending a KIM (Kommunikation im Medizinwesen) message to a doctor.
   */
  async sendKIMMessage(recipientKimAddress: string, subject: string, body: string, attachment?: Blob) {
    console.log(`[TIaaS] Sending KIM message to ${recipientKimAddress}...`);
    await new Promise(resolve => setTimeout(resolve, 1500));
    
    return {
      success: true,
      messageId: `kim-${Math.random().toString(36).substring(7)}`,
      timestamp: new Date().toISOString()
    };
  }
}

export const tiService = new TIService();
