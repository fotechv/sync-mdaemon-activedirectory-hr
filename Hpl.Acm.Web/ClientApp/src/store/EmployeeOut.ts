import { Action, Reducer } from "redux";
import { AppThunkAction } from ".";
import { IEmployeeModel } from "../models/Employee";

// -----------------
// STATE - This defines the type of data maintained in the Redux store.
export interface IEmployeesOutState {
  isLoading: boolean;
  startDateIndex?: number;
  employeesOut: IEmployeeModel[];
}

// -----------------
// ACTIONS - These are serializable (hence replayable) descriptions of state transitions.
// They do not themselves have any side-effects; they just describe something that is going to happen.

interface IRequestEmployeesOutAction {
  type: "REQUEST_EMPLOYEES_OUT";
  startDateIndex: number;
}

interface IReceiveEmployeesOutAction {
  type: "RECEIVE_EMPLOYEES_OUT";
  startDateIndex: number;
  employeesOut: IEmployeeModel[];
}

// Declare a 'discriminated union' type. This guarantees that all references to 'type' properties contain one of the
// declared type strings (and not any other arbitrary string).
type KnownAction = IRequestEmployeesOutAction | IReceiveEmployeesOutAction;

// ----------------
// ACTION CREATORS - These are functions exposed to UI components that will trigger a state transition.
// They don't directly mutate state, but they can have external side-effects (such as loading data).

export const actionCreators = {
  requestEmployees:
    (startDateIndex: number): AppThunkAction<KnownAction> =>
    (dispatch, getState) => {
      // Only load data if it's something we don't already have (and are not already loading)
      const appState = getState();
      if (
        appState &&
        appState.employeesOut &&
        startDateIndex !== appState.employeesOut.startDateIndex
      ) {
        fetch(`api/employee/GetAllNhanVienNghiViec`)
          .then((response) => {
            return response.json() as Promise<IEmployeeModel[]>;
          })
          .then((data) => {
            // console.log(data.Payload);
            dispatch({
              type: "RECEIVE_EMPLOYEES_OUT",
              startDateIndex: startDateIndex,
              employeesOut: data,
            });
          });

        dispatch({
          type: "REQUEST_EMPLOYEES_OUT",
          startDateIndex: startDateIndex,
        });
      }
    },
};

// ----------------
// REDUCER - For a given state and action, returns the new state. To support time travel, this must not mutate the old state.

const unloadedState: IEmployeesOutState = {
  employeesOut: [],
  isLoading: false,
};

export const reducer: Reducer<IEmployeesOutState> = (
  state: IEmployeesOutState | undefined,
  incomingAction: Action
): IEmployeesOutState => {
  if (state === undefined) {
    return unloadedState;
  }

  const action = incomingAction as KnownAction;
  switch (action.type) {
    case "REQUEST_EMPLOYEES_OUT":
      return {
        startDateIndex: action.startDateIndex,
        employeesOut: state.employeesOut,
        isLoading: true,
      };
    case "RECEIVE_EMPLOYEES_OUT":
      // Only accept the incoming data if it matches the most recent request. This ensures we correctly
      // handle out-of-order responses.
      if (action.startDateIndex === state.startDateIndex) {
        return {
          startDateIndex: action.startDateIndex,
          employeesOut: action.employeesOut,
          isLoading: false,
        };
      }
      break;
  }

  return state;
};
