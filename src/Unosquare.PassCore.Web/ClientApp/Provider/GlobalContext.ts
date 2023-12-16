import * as React from 'react';
import { IGlobalContext, ISnackbarContext } from '../types/Providers';

export const GlobalContext = React.createContext<IGlobalContext>({
    alerts: null,
    applicationTitle: '',
    changePasswordForm: null,
    changePasswordTitle: '',
    errorsPasswordForm: null,
    reCaptcha: null,
    showPasswordMeter: false,
    useEmail: false,
    validationRegex: null,
    usePasswordGeneration: false,
});

export const SnackbarContext = React.createContext<ISnackbarContext>({
    sendMessage: null,
});
